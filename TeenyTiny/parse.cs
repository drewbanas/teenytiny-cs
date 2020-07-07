using lex;
using emit;
using util;

namespace parse
{
    // Parser object keeps track of current token and check if the code matches the grammar
    class Parser
    {
        private Lexer lexer;
        private Emitter emitter;

        System.Collections.Generic.HashSet<string> symbols;
        System.Collections.Generic.HashSet<string> labelsDeclared;
        System.Collections.Generic.HashSet<string> labelsGotoed;

        Token curToken;
        Token peekToken;

        public Parser(Lexer lexer, Emitter emitter)
        {
            this.lexer = lexer;
            this.emitter = emitter;

            this.symbols = new System.Collections.Generic.HashSet<string>(); // All variables we have declared so far.
            this.labelsDeclared = new System.Collections.Generic.HashSet<string>(); // Keep track of all labels declared
            this.labelsGotoed = new System.Collections.Generic.HashSet<string>(); // All labels goto'ed, so we know if they exist or not

            this.curToken = null;
            this.peekToken = null;
            this.nextToken();
            this.nextToken(); // Call this twice to initialize current and peek.
        }

        // Return true if the curent token matches.
        bool checkToken(TokenType kind)
        {
            return kind == this.curToken.kind;
        }

        // Return true if the next token matches.
        bool checkPeek(TokenType kind)
        {
            return kind == this.peekToken.kind;
        }

        // Try to match the current token. If not, error. Avances the current token.
        void match(TokenType kind)
        {
            if (!this.checkToken(kind))
                this.abort("Expected " + kind.ToString() + ", got" + this.curToken.kind.ToString());

            this.nextToken();
        }

        // Advances the current token.
        void nextToken()
        {
            this.curToken = this.peekToken;
            this.peekToken = this.lexer.getToken();
            // No need to worry about passing the EOF, lexer handles that.
        }

        // Return true if the current token is a comparison operator
        bool isComparisonOperator()
        {
            return
                this.checkToken(TokenType.GT) || this.checkToken(TokenType.GTEQ) ||
                this.checkToken(TokenType.LT) || this.checkToken(TokenType.LTEQ) ||
                this.checkToken(TokenType.EQEQ) || this.checkToken(TokenType.NOTEQ);
        }

        void abort(string message)
        {
            pfun.Exit("Error! " + message);
        }

        // Production rules.

        // program ::= {statment}
        public void program()
        {
            this.emitter.headerLine("#include <stdio.h>");
            this.emitter.headerLine("int main(void){");

            // Since some newlines are required in our grammar, need to skip the excess.
            while (this.checkToken(TokenType.NEWLINE))
                this.nextToken();

            // Parse all the statements in the program.
            while (!this.checkToken(TokenType.EOF))
                this.statement();

            // Wrap things up.
            this.emitter.emitLine("return 0;");
            this.emitter.emitLine("}");

            // Check that each label referenced in a GOTO is declared.
            foreach (string label in this.labelsGotoed)
                if (!labelsDeclared.Contains(label))
                    this.abort("Attempting to GOTO to undeclared label: " + label);
        }

        // One of the following statements...
        void statement()
        {
            // Check the first token to see what kind of statement this is.

            // "PRINT" (expression | string)
            if (this.checkToken(TokenType.PRINT))
            {
                this.nextToken();

                if (this.checkToken(TokenType.STRING))
                {
                    // Simple string, so print it.
                    this.emitter.emitLine("printf(\"" + this.curToken.text + "\\n\");");
                    this.nextToken();
                }
                else
                {
                    // Expect an expression and printhte result as a float.
                    this.emitter.emit("printf(\"%" + ".2f\\n\", (float)(");
                    this.expression();
                    this.emitter.emitLine("));");
                }
            }

            // "IF" comparison "THEN" block "ENDIF"
            else if (this.checkToken(TokenType.IF))
            {
                this.nextToken();
                this.emitter.emit("if(");
                this.comparison();

                this.match(TokenType.THEN);
                this.nl();
                this.emitter.emitLine("){");

                // Zero or more statements in the body
                while (!this.checkToken(TokenType.ENDIF))
                    this.statement();

                this.match(TokenType.ENDIF);
                this.emitter.emitLine("}");
            }

            // "WHILE" comparison "REPEAT" block "ENDWHILE"
            else if (this.checkToken(TokenType.WHILE))
            {
                this.nextToken();
                this.emitter.emit("while(");
                this.comparison();

                this.match(TokenType.REPEAT);
                this.nl();
                this.emitter.emitLine("){");

                // Zero or more statements in the loop body
                while (!this.checkToken(TokenType.ENDWHILE))
                    this.statement();

                this.match(TokenType.ENDWHILE);
                this.emitter.emitLine("}");
            }

            // "LABEL" ident
            else if (this.checkToken(TokenType.LABEL))
            {
                this.nextToken();

                // Make sure this label doesn't already exist.
                if (labelsDeclared.Contains(this.curToken.text))
                    this.abort("Label already exists: " + this.curToken.text);
                this.labelsDeclared.Add(this.curToken.text);

                this.emitter.emitLine(this.curToken.text + ":");
                this.match(TokenType.IDENT);
            }

            // "GOTO" ident
            else if (this.checkToken(TokenType.GOTO))
            {
                this.nextToken();
                this.labelsGotoed.Add(this.curToken.text);
                this.emitter.emitLine("goto " + this.curToken.text + ";");
                this.match(TokenType.IDENT);
            }

            // "LET" ident "=" expression
            else if (this.checkToken(TokenType.LET))
            {
                this.nextToken();

                // Check if ident exists in symbol table. If not, declare it.
                if (!this.symbols.Contains(this.curToken.text))
                {
                    this.symbols.Add(this.curToken.text);
                    this.emitter.headerLine("float " + this.curToken.text + ";");
                }

                this.emitter.emit(this.curToken.text + " = ");
                this.match(TokenType.IDENT);
                this.match(TokenType.EQ);
                this.expression();
                this.emitter.emitLine(";");
            }

            // "INPUT" ident
            else if (this.checkToken(TokenType.INPUT))
            {
                this.nextToken();

                // If variable doesn't already exist, declare it.
                if (!this.symbols.Contains(this.curToken.text))
                {
                    this.symbols.Add(this.curToken.text);
                    this.emitter.headerLine("float " + this.curToken.text + ";");
                }

                // Emit scanf but also validate the input. If invalid, set the variable to 0 and clear the input.
                this.emitter.emitLine("if(0 == scanf(\"%" + "f\", &" + this.curToken.text + ")) {");
                this.emitter.emitLine(this.curToken.text + " = 0;");
                this.emitter.emit("scanf(\"%");
                this.emitter.emitLine("*s\");");
                this.emitter.emitLine("}");
                this.match(TokenType.IDENT);
            }

            // This is not a valid statement. Error!
            else
            {
                this.abort("Invalid statement at " + this.curToken.text + " (" + this.curToken.kind.ToString() + ")");
            }

            // Newline.
            this.nl();
        }

        // comparison ::= expression (("==" | "!=" | ">" | ">=" | "<" | "<=") expression)+
        void comparison()
        {
            this.expression();
            // Must be at least one comparison operator and another expression.
            if (this.isComparisonOperator())
            {
                this.emitter.emit(this.curToken.text);
                this.nextToken();
                this.expression();
            }
            // Can have 0 or more comparison operator and expressions.
            while (this.isComparisonOperator())
            {
                this.emitter.emit(this.curToken.text);
                this.nextToken();
                this.expression();
            }
        }

        // expression ::= term {( "-" | "+" ) term}
        void expression()
        {
            this.term();
            // Can have 0 or more +/- and expressions.
            while (this.checkToken(TokenType.PLUS) || this.checkToken(TokenType.MINUS))
            {
                this.emitter.emit(this.curToken.text);
                this.nextToken();
                this.term();
            }
        }

        // term ::= unary {( "/" | "*" ) unary}
        void term()
        {
            this.unary();
            // Can have 0 or more +// and expressions.
            while (this.checkToken(TokenType.ASTERISK) || this.checkToken(TokenType.SLASH))
            {
                this.emitter.emit(this.curToken.text);
                this.nextToken();
                this.unary();
            }
        }

        // unary ::= ["+" | "-"] primary
        void unary()
        {
            // Optional unary +/-
            if (this.checkToken(TokenType.PLUS) || this.checkToken(TokenType.MINUS))
            {
                this.emitter.emit(this.curToken.text);
                this.nextToken();
            }
            this.primary();
        }

        // primary ::= number | ident
        void primary()
        {
            if (this.checkToken(TokenType.NUMBER))
            {
                this.emitter.emit(this.curToken.text);
                this.nextToken();
            }
            else if (this.checkToken(TokenType.IDENT))
            {
                // Ensure the variable already exists.
                if (!this.symbols.Contains(this.curToken.text))
                    this.abort("Referencing variable before assignment: " + this.curToken.text);

                this.emitter.emit(this.curToken.text);
                this.nextToken();
            }
            else
            {
                // Error!
                this.abort("Unexpected token at " + this.curToken.text);
            }
        }

        // nl ::= '\n'+
        void nl()
        {
            // Reguire at least one newline
            this.match(TokenType.NEWLINE);
            // But we will allow extra newlines too, of course
            while (this.checkToken(TokenType.NEWLINE))
                this.nextToken();
        }
    }
}
