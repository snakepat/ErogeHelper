﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using ErogeHelper.Common;
using ErogeHelper.Common.Enum;
using ErogeHelper.Common.Extention;
using ErogeHelper.Common.Messenger;
using ErogeHelper.Model.Repository;
using ErogeHelper.ViewModel.Window;
using Microsoft.Extensions.DependencyInjection;

namespace ErogeHelper.ViewModel.Page
{
    public class GeneralViewModel : PropertyChangedBase, IHandle<PageNavigatedMessage>
    {
        public GeneralViewModel(
            EhConfigRepository ehConfigRepository, 
            IEventAggregator eventAggregator, 
            IServiceProvider serviceProvider,
            GameRuntimeDataRepo gameRuntimeDataRepo)
        {
            _ehConfigRepository = ehConfigRepository;
            _eventAggregator = eventAggregator;
            _serviceProvider = serviceProvider;
            _gameRuntimeDataRepo = gameRuntimeDataRepo;

            _eventAggregator.SubscribeOnUIThread(this);
        }

        private readonly EhConfigRepository _ehConfigRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly IServiceProvider _serviceProvider;
        private readonly GameRuntimeDataRepo _gameRuntimeDataRepo;

        private string _diskUsageProgressBarText = string.Empty;
        private double _diskUsageProgressBarValue;

        public bool OutsideWindow
        {
            get => _ehConfigRepository.UseMoveableTextControl;
            set
            {
                if (value)
                {
                    _eventAggregator.PublishOnUIThreadAsync(new UseMoveableTextMessage {UseMove = true});
                }
                else
                {
                    _eventAggregator.PublishOnUIThreadAsync(new UseMoveableTextMessage {UseMove = false});
                }
                _ehConfigRepository.UseMoveableTextControl = value;
            }
        }

        public bool DeepLExtention
        {
            get => _ehConfigRepository.PasteToDeepL;
            set => _ehConfigRepository.PasteToDeepL = value;
        }

        public void SetDefaultEhConfig()
        {
            DeepLExtention = false;
            NotifyOfPropertyChange(() => DeepLExtention);
            _ehConfigRepository.ClearConfig();
        }

        public void CustomButton()
        {
            //Log.Debug(_serviceProvider.GetServices<PreferenceViewModel>().ToList().Count.ToString());
            _ = IoC.Get<IWindowManager>().ShowWindowFromIoCAsync<HookConfigViewModel>();
        }

        public string DiskUsageProgressBarText
        {
            get => _diskUsageProgressBarText;
            set { _diskUsageProgressBarText = value; NotifyOfPropertyChange(() => DiskUsageProgressBarText);}
        }

        public double DiskUsageProgressBarValue
        {
            get => _diskUsageProgressBarValue;
            set { _diskUsageProgressBarValue = value; NotifyOfPropertyChange(() => DiskUsageProgressBarValue);}
        }

        private void GetDiskUsage()
        {
            var (availableFreeSpace, totalSize) = Utils.GetDiskUsage(_ehConfigRepository.AppDataDir);
            if (totalSize != 0)
            {
                var cacheSpace = Utils.GetDirectorySize(_ehConfigRepository.AppDataDir);
                DiskUsageProgressBarText = $@"{Utils.CountSize(cacheSpace)}/{Utils.CountSize(availableFreeSpace + cacheSpace)}";
                var percentage = cacheSpace / (availableFreeSpace + cacheSpace);
                DiskUsageProgressBarValue = percentage * 100;
            }
            else
            {
                DiskUsageProgressBarText = string.Empty;
                DiskUsageProgressBarValue = 0;
            }
        }

        public Task HandleAsync(PageNavigatedMessage message, CancellationToken cancellationToken)
        {
            if (message.Page == PageName.General)
            {
                GetDiskUsage();
            }

            return Task.CompletedTask;
        }

#pragma warning disable CS8618
        public GeneralViewModel() { }
    }
}