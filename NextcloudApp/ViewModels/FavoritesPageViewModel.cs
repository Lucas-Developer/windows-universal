﻿using System.Collections.Generic;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudClient.Types;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;

namespace NextcloudApp.ViewModels
{
    public class FavoritesPageViewModel : DirectoryListPageViewModel
    {
        private ResourceInfo _selectedFileOrFolder;
        private readonly INavigationService _navigationService;
        private bool _isNavigatingBack;

        public FavoritesPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
            : base(navigationService, resourceLoader, dialogService)
        {
            _navigationService = navigationService;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            Directory = DirectoryService.Instance;
            StartDirectoryListing();
            _isNavigatingBack = false;

            if (e.Parameter == null)
            {
                return;
            }
            var parameter = FileInfoPageParameters.Deserialize(e.Parameter);
            SelectedFileOrFolder = parameter?.ResourceInfo;
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            _isNavigatingBack = true;
            if (!suspending)
            {
                Directory.StopDirectoryListing();
                Directory = null;
                _selectedFileOrFolder = null;
            }
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        public override ResourceInfo SelectedFileOrFolder
        {
            get => _selectedFileOrFolder;
            set
            {
                if (Directory != null && Directory.IsSelecting)
                {
                    return;
                }
                if (_isNavigatingBack)
                {
                    return;
                }
                try
                {
                    if (!SetProperty(ref _selectedFileOrFolder, value))
                    {
                        return;
                    }
                }
                catch
                {
                    return;
                }

                if (value == null)
                {
                    return;
                }

                if (Directory?.PathStack == null)
                {
                    return;
                }

                if (Directory.IsSorting)
                {
                    return;
                }
                if (value.IsDirectory)
                {
                    var parameters = new FileInfoPageParameters
                    {
                        ResourceInfo = value
                    };

                    if (parameters.ResourceInfo != null)
                    {
                        Directory.PathStack.Clear();

                        Directory.PathStack.Add(new PathInfo
                        {
                            ResourceInfo = new ResourceInfo()
                            {
                                Name = "Nextcloud",
                                Path = "/"
                            },
                            IsRoot = true
                        });

                        string[] pathSplit = value.Path.Split('/');
                        foreach (string pathPart in pathSplit)
                        {
                            if (pathPart.Length > 0)
                            {
                                Directory.PathStack.Add(new PathInfo
                                {
                                    ResourceInfo = new ResourceInfo()
                                    {
                                        Name = pathPart,
                                        Path = "/" + ((Directory.PathStack[Directory.PathStack.Count - 1]).ResourceInfo.Path + "/" + pathPart).TrimStart('/')
                                    },
                                    IsRoot = false
                                });
                            }
                        }
                    }
                    _navigationService.Navigate(PageToken.DirectoryList.ToString(), null);
                }
                else
                {
                    var parameters = new FileInfoPageParameters
                    {
                        ResourceInfo = value
                    };
                    _navigationService.Navigate(PageToken.FileInfo.ToString(), parameters.Serialize());
                }
            }
        }
        
        private async void StartDirectoryListing()
        {
            ShowProgressIndicator();

            await Directory.StartDirectoryListing(null, "favorites");

            HideProgressIndicator();
            SelectedFileOrFolder = null;
            // ReSharper disable once ExplicitCallerInfoArgument
            RaisePropertyChanged(nameof(StatusBarText));
        }
    }
}