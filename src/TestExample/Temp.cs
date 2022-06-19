using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExample
{
    public  class Temp
    {
        public void Define()
        {
            int boundInt = 0;
            FileInfo[] boundFiles = null;

            var root = new RootCommand()
                .Setup()   //Setup returns something we can put extensions on
                    .AddOption<int>("--i")
                    .AddArgument < FileInfo[]>(),
            ...

        }
    }
}
