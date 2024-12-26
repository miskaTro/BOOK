using System;
using Newtonsoft.Json;

namespace BookCollectionApp
{
    public class Book
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Year { get; set; }

        // Поле для хранения файла в виде массива байтов
        public byte[] FileData { get; set; }


    }
}