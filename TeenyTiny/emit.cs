namespace emit
{
    // Emitter object keeps track of the generated code and outputs it.
    class Emitter
    {
        string fullPath;
        string header;
        string code;

        public Emitter(string fullPath)
        {
            this.fullPath = fullPath;
            this.header = "";
            this.code = "";
        }

        public void emit(string code)
        {
            this.code += code; // suggestion: use stringbuilder
        }

        public void emitLine(string code)
        {
            this.code += code + '\n';
        }

        public void headerLine(string code)
        {
            this.header += code + '\n';
        }

        public void writeFile()
        {
            using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(this.fullPath))
            {
                outputFile.Write(this.header + this.code);
            }
        }
    }
}
