using util;

namespace lex
{
    class Lexer
    {
        string source;
        public char curChar;
        int curPos;

        // Lexer object keeps track of current position in the source code and produces each token.
        public Lexer(string input)
        {
            this.source = input + "\n"; // Source code to lex as a string. Append newline to simplify lexing/parsing.
            this.curChar = '\0'; //  Current character in the string.
            this.curPos = -1; //  Current position in the string.
            nextChar();
        }

        // Process the next character
        public void nextChar()
        {
            this.curPos += 1;
            if (this.curPos >= this.source.Length)
            {
                this.curChar = '\0'; // EOF
            }
            else
            {
                this.curChar = this.source[this.curPos];
            }
        }

        // Return the lookahead character
        public char peek()
        {
            if (this.curPos + 1 >= this.source.Length)
                return '\0';

            return this.source[this.curPos + 1];

        }

        // Invalid token found, print error message and exit
        public void abort(string message)
        {
            System.Console.WriteLine("Lexing error. " + message);
            System.Console.ReadKey(); // Convenience pause
            System.Environment.Exit(-1);
        }

        // Return the next token
        public Token getToken()
        {
            this.skipWhitespace();
            this.skipComment();
            /*
             * Check the first character of this token to see if we can decide what it is
             * If it is a multiple character operator (e.g., !=), number, identier or keyword, then we will proceed
             */
            Token token = null;

            if (this.curChar == '+') // Plus token
            {
                token = new Token(this.curChar.ToString(), TokenType.PLUS);
            }
            else if (this.curChar == '-') // Minus token
            {
                token = new Token(this.curChar.ToString(), TokenType.MINUS);
            }
            else if (this.curChar == '*')// Asterisk token
            {
                token = new Token(this.curChar.ToString(), TokenType.ASTERISK);
            }
            else if (this.curChar == '/') // Slash token
            {
                token = new Token(this.curChar.ToString(), TokenType.SLASH);
            }
            else if (this.curChar == '=')
            {
                // Check if token is = or ==
                if (this.peek() == '=')
                {
                    char lastChar = this.curChar;
                    this.nextChar();
                    token = new Token(lastChar.ToString() + this.curChar, TokenType.EQEQ);
                }
                else
                {
                    token = new Token(this.curChar.ToString(), TokenType.EQ);
                }
            }
            else if (this.curChar == '>')
            {
                // Check if token is > or >=
                if (this.peek() == '=')
                {
                    char lastChar = this.curChar;
                    this.nextChar();
                    token = new Token(lastChar.ToString() + this.curChar, TokenType.GTEQ);
                }
                else
                {
                    token = new Token(this.curChar.ToString(), TokenType.GT);
                }
            }
            else if (this.curChar == '<')
            {
                // Check if token is < or <=
                if (this.peek() == '=')
                {
                    char lastChar = this.curChar;
                    this.nextChar();
                    token = new Token(lastChar.ToString() + this.curChar, TokenType.LTEQ);
                }
                else
                {
                    token = new Token(this.curChar.ToString(), TokenType.LT);
                }
            }
            else if (this.curChar == '!')
            {
                if (this.peek() == '=')
                {
                    char lastChar = this.curChar;
                    this.nextChar();
                    token = new Token(lastChar.ToString() + this.curChar, TokenType.NOTEQ);
                }
                else
                {
                    this.abort("Expected !=, got !" + this.peek());
                }
            }
            else if (this.curChar == '\"')
            {
                // Get characters between quotations.
                this.nextChar();
                int startPos = this.curPos;

                while (this.curChar != '\"')
                {
                    // Don't allow special characters in string. No escape characters, newline, tabs, or %.
                    // We will be using C's printf on this string.
                    if (this.curChar == '\r' || this.curChar == '\n' || this.curChar == '\t' || this.curChar == '\\' || this.curChar == '%')
                        this.abort("Illegal character in string.");
                    this.nextChar();
                }

                string tokText = this.source.Substring(startPos, this.curPos - startPos);
                token = new Token(tokText, TokenType.STRING);
            }
            else if (pfun.isDigit(this.curChar))
            {
                // Leading character is a digit, so this must be a number.
                // Get all consecutive digits and decimal if there is one.
                int startPos = this.curPos;
                while (pfun.isDigit(this.peek()))
                    this.nextChar();

                if (this.peek() == '.') // Decimal!
                {
                    this.nextChar();

                    // Must have at least one digit after decimal.
                    if (!pfun.isDigit(this.peek())) // Error!
                        this.abort("Illegal character in number");

                    while (pfun.isDigit(this.peek()))
                        this.nextChar();
                }

                string tokText = source.Substring(startPos, this.curPos - startPos + 1); // Get the substring.
                token = new Token(tokText, TokenType.NUMBER);
            }
            else if (pfun.isAlpha(this.curChar))
            {
                // Leading character is a letter, so this must be an identifier or keyword.
                // Get all consecutive alphanumeric characters
                int startPos = this.curPos;
                while (pfun.isAlNum(this.peek()))
                    this.nextChar();

                // Check if the token is in the list of keywords.
                string tokText = source.Substring(startPos, this.curPos - startPos + 1); // Get the substrin
                TokenType keyword = Token.checkIfKeyword(tokText);
                if (keyword == TokenType.NONE) // Identifier
                    token = new Token(tokText, TokenType.IDENT);
                else // Keyword
                    token = new Token(tokText, keyword);
            }
            else if (this.curChar == '\n') // Newline token
            {
                token = new Token(this.curChar.ToString(), TokenType.NEWLINE);
            }
            else if (this.curChar == '\0') // EOF token
            {
                token = new Token(this.curChar.ToString(), TokenType.EOF);
            }
            else
            {
                // Unknown token!
                abort("Unknown token: " + this.curChar);
            }

            this.nextChar();
            return token;
        }

        // Skip whitespaces except newlines, which we will use to indicate the end of a statement
        public void skipWhitespace()
        {
            while (this.curChar == ' ' || this.curChar == '\t' || this.curChar == '\r')
                this.nextChar();
        }

        // Skip comments in code
        public void skipComment()
        {
            if (this.curChar == '#')
                while (this.curChar != '\n')
                    this.nextChar();
        }

    }

    // Token contains the original text and the type of token.
    class Token
    {
        public string text;
        public TokenType kind;

        public Token(string tokenText, TokenType tokenKind)
        {
            this.text = tokenText; // The token's actual text. Used for identifiers, strings, and numbers.
            this.kind = tokenKind; // The TokenType that this token is classified as.
        }

        public static TokenType checkIfKeyword(string tokText)
        {
            foreach (int value in TokenType.GetValues(typeof(TokenType)))
            {
                TokenType kind = (TokenType)value;
                // Relies on all keyword enum values being 1XX.
                if (string.Equals(kind.ToString(), tokText) && value >= 100 && value < 200)
                    return kind;
            }

            return TokenType.NONE;
        }
    }

    public enum TokenType
    {
        NONE = -9, // Work around since enum cannot be null (None in Python)
        EOF = -1,
        NEWLINE = 0,
        NUMBER = 1,
        IDENT = 2,
        STRING = 3,
        // Keywords
        LABEL = 101,
        GOTO = 102,
        PRINT = 103,
        INPUT = 104,
        LET = 105,
        IF = 106,
        THEN = 107,
        ENDIF = 108,
        WHILE = 109,
        REPEAT = 110,
        ENDWHILE = 111,
        // Operators
        EQ = 201,
        PLUS = 202,
        MINUS = 203,
        ASTERISK = 204,
        SLASH = 205,
        EQEQ = 206,
        NOTEQ = 207,
        LT = 208,
        LTEQ = 209,
        GT = 210,
        GTEQ = 211
    }
}
