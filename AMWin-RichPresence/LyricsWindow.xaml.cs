using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AMWin_RichPresence {
    public partial class LyricsWindow : Window {
        private List<LrcLine> currentLyrics = new List<LrcLine>();

        public LyricsWindow() {
            InitializeComponent();
        }

        public void SetLyrics(List<LrcLine> lyrics) {
            currentLyrics = lyrics;
        }

        public void SetStatus(string msg) {
            TextBlock_Current.Text = msg;
            TextBlock_Prev.Text = "";
            TextBlock_Next.Text = "";
        }

        public void UpdateLyrics(double currentTimeSeconds) {
            if (currentLyrics.Count == 0) return;

            TimeSpan current = TimeSpan.FromSeconds(currentTimeSeconds);

            int currentIndex = -1;
            for (int i = 0; i < currentLyrics.Count; i++) {
                if (currentLyrics[i].Time <= current) {
                    currentIndex = i;
                } else {
                    break;
                }
            }

            if (currentIndex != -1) {
                var currentLine = currentLyrics[currentIndex].Text;
                
                // Show dots if empty
                if (string.IsNullOrWhiteSpace(currentLine)) {
                    currentLine = "•••";
                }
                
                TextBlock_Current.Text = currentLine;
                TextBlock_Prev.Text = currentIndex > 0 ? currentLyrics[currentIndex - 1].Text : "";
                TextBlock_Next.Text = currentIndex < currentLyrics.Count - 1 ? currentLyrics[currentIndex + 1].Text : "";
            } else {
                // Intro phase
                TextBlock_Prev.Text = "";
                TextBlock_Current.Text = "•••"; 
                TextBlock_Next.Text = currentLyrics.Count > 0 ? currentLyrics[0].Text : "";
            }
        }
    }
}
