using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wordsParser
{
    class Word
    {
        private string word;
        private int quantity;

        public Word(string _word)
        {
            WD = _word;
            Quantity = 1;
        }

        /// <summary>
        /// Word
        /// </summary>
        public string WD { get => word; set => word = value; }
        /// <summary>
        /// Quantity
        /// </summary>
        public int Quantity { get => quantity; set => quantity = value; }
    }
}
