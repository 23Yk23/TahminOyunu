using System;
using EntityLayer.Concrete; // MediaImage sınıfınız burada olduğu için
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TahminOyunu.Models // YourProjectName kısmını kendi projenizin namespace'i ile değiştirin
{
    public class PlayGameViewModel
    {
        public int MediaId { get; set; }
        public string MediaTitle { get; set; }//eklendi
        public int SelectedIndex { get; set; } = 0; //eklendi
        public int? PreviousMediaId { get; set; }//eklendi
        public int CurrentGameNumber { get; set; }//eklendi

        public int? NextMediaId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MediaImage> AllImages { get; set; } = new List<MediaImage>();
        public string CurrentImagePath { get; set; }
        public int CurrentImageIndex { get; set; }
        public int Attempts { get; set; }
        public string UserGuess { get; set; } // Formdan gelecek tahmin
        public bool IsCorrect { get; set; }
        public bool GameOver { get; set; }
        public string Message { get; set; }
        public int MaxAttempts { get; } = 6;
    }
}