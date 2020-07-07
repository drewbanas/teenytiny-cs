using lex;
using emit;
using parse;
using util;

namespace TeenyTiny
{
    class teenytiny
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Teeny Tiny Compiler");

            if (args.Length != 1)
                pfun.Exit("Error: Compiler needs source file as argument.");

            string input = System.IO.File.ReadAllText(args[0]);

            // Initialize the lexer and parser.
            Lexer lexer = new Lexer(input);
            Emitter emitter = new Emitter("out.c");
            Parser parser = new Parser(lexer, emitter);

            parser.program(); // Start the parser.
            emitter.writeFile(); // Write the output to file.
            System.Console.WriteLine("Compiling completed.");

        }
    }
}
