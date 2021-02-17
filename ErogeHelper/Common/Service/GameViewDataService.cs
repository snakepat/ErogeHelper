﻿using Caliburn.Micro;
using ErogeHelper.Common.Helper;
using ErogeHelper.Model;
using ErogeHelper.Model.Translator;
using ErogeHelper.ViewModel;
using ErogeHelper.ViewModel.Control;
using ErogeHelper.ViewModel.Pages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ErogeHelper.Common.Service
{
    class GameViewDataService : IGameViewDataService
    {
        public event IGameViewDataService.SourceDataEventHandler? SourceDataEvent;
        public event IGameViewDataService.AppendDataEventHandler? AppendDataEvent;

        public GameViewModel gameViewModel { get; set; } = null!;
        public MecabViewModel mecabViewModel { get; set; } = null!;
        private readonly MecabHelper mecabHelper;

        public GameViewDataService(MecabHelper mecabHelper)
        {
            this.mecabHelper = mecabHelper;

            if (mecabHelper.CanCreateTagger)
            {
                mecabHelper.CreateTagger();
            }
            else
            {
                if (DataRepository.EnableMecab)
                {
                    DataRepository.EnableMecab = false;
                }
            }    
        }

        public void Start()
        {
            Textractor.SelectedDataEvent += DataProcess;
        }

        private void DataProcess(object sender, HookParam hp)
        {
            // Refresh
            gameViewModel.AppendTextList.Clear();

            // User define RegExp 
            var pattern = GameConfig.RegExp;
            if (!string.IsNullOrEmpty(pattern))
            {
                var list = Regex.Split(hp.Text, pattern);
                hp.Text = string.Join("", list);
            }

            // Clear ascii control characters
            hp.Text = new string(hp.Text.Select(c => c < ' ' ? '_' : c).ToArray()).Replace("_", string.Empty);
            // Linebreak 
            // Full-width space
            hp.Text = hp.Text.Replace("　", string.Empty);
            // Ruby like <.*?>

            if (hp.Text.Length > 120)
            {
                gameViewModel.TextControl.SourceTextCollection.Clear();
                AppendDataEvent?.Invoke(typeof(GameViewDataService), Language.Strings.GameView_MaxLenthTip, string.Empty);
                return;
            }

            // DeepL Extension
            if (DataRepository.PasteToDeepL)
            {
                Process[] temp = Process.GetProcessesByName("DeepL");
                if (temp.Length != 0)
                {
                    IntPtr handle = temp[0].MainWindowHandle;
                    NativeMethods.SwitchToThisWindow(handle);

                    // Do SetText and Paste both
                    new DeepLHelper(DataFormats.Text, hp.Text).Go();

                    if (NativeMethods.GetForegroundWindow() != handle)
                    {
                        // Better use Toast in win10
                        Application.Current.Dispatcher.InvokeAsync(() => ModernWpf.MessageBox.Show(
                            "Didn't find DeepL client in front, will turn off DeepL extension..", "Eroge Helper"));

                        DataRepository.PasteToDeepL = false;
                    }
                }
            }

            gameViewModel.SourceTextArchiver.Enqueue(hp.Text);
            gameViewModel.TextControl.CardControl.DisplayedText = hp.Text;

            // Process source japanese text
            if (mecabViewModel.MecabToggle)
            {
                var collect = Utils.BindableTextMaker(mecabHelper.IpaDicParser(hp.Text));
                SourceDataEvent?.Invoke(typeof(GameViewDataService), collect);
            }

            foreach(var translator in TranslatorManager.GetEnabled())
            {
                Task.Run(async () =>
                {
                    Stopwatch sw = new();
                    sw.Start();
                    var result = await translator.TranslateAsync(hp.Text);
                    sw.Stop();
                    if (!result.Equals(string.Empty))
                    {
                        Log.Debug($"{translator.Name}: {result}");
                        AppendDataEvent?.Invoke(typeof(GameViewDataService), result, $"{translator.Name} {sw.ElapsedMilliseconds}ms");
                    }
                });
            }

            // hard code for sakura no uta
            //if (GameConfig.MD5.Equals("BAB61FB3BD98EF1F1538EE47A8A46A26"))
            //{
            //    string result = sakuraNoUtaHelper.QueryText(hp.Text);
            //    if (!string.IsNullOrWhiteSpace(result))
            //        AppendDataEvent?.Invoke(typeof(GameViewDataService), result);
            //}
        }

        public Visibility GetPinToggleVisibility() => DataRepository.EnableMecab ? Visibility.Visible : Visibility.Collapsed;

        public void RefreshCurentMecabText(string text = "")
        {
            if (text.Equals(string.Empty))
            {
                // maybe should never happen
                text = gameViewModel.SourceTextArchiver.Last();
            }

            if (mecabViewModel.MecabToggle)
            {
                var collect = Utils.BindableTextMaker(mecabHelper.IpaDicParser(text));
                SourceDataEvent?.Invoke(typeof(GameViewDataService), collect);
            }
        }
    }
}
