﻿using System;
using CodeBucket.Core.Services;
using System.Reactive.Linq;
using ReactiveUI;
using System.Reactive;
using Splat;
using CodeBucket.Client.V1;

namespace CodeBucket.Core.ViewModels.Repositories
{
    public class ReadmeViewModel : BaseViewModel, ILoadableViewModel
    {
        private string _htmlUrl;

        private FileModel _contentModel;
        public FileModel ContentModel
        {
            get { return _contentModel; }
            private set { this.RaiseAndSetIfChanged(ref _contentModel, value); }
        }

        private string _contentText;
        public string ContentText
        {
            get { return _contentText; }
            private set { this.RaiseAndSetIfChanged(ref _contentText, value); }
        }

        public ReactiveCommand<Unit, Unit> ShowMenuCommand { get; }

        public ReactiveCommand<Unit, Unit> LoadCommand { get; }

        public ReadmeViewModel(
            string username, string repository, string filename,
            IApplicationService applicationService = null, 
            IMarkdownService markdownService = null,
            IActionMenuService actionMenuService = null)
        {
            applicationService = applicationService ?? Locator.Current.GetService<IApplicationService>();
            markdownService = markdownService ?? Locator.Current.GetService<IMarkdownService>();
            actionMenuService = actionMenuService ?? Locator.Current.GetService<IActionMenuService>();

            var canShowMenu = this.WhenAnyValue(x => x.ContentModel).Select(x => x != null);

            ShowMenuCommand = ReactiveCommand.CreateFromTask(sender => 
            {
                var menu = actionMenuService.Create();
                menu.AddButton("Share", x => actionMenuService.ShareUrl(x, new Uri(_htmlUrl)));
                menu.AddButton("Show in Bitbucket", _ => NavigateTo(new WebBrowserViewModel(_htmlUrl)));
                return menu.Show(sender);
            }, canShowMenu);

            Title = "Readme";

            LoadCommand = ReactiveCommand.CreateFromTask(async t =>
            {
                var filepath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
                var mainBranch = (await applicationService.Client.Repositories.GetPrimaryBranch(username, repository)).Name;
                ContentModel = await applicationService.Client.Repositories.GetFile(username, repository, mainBranch, filename);

                var readme = ContentModel.Data;
                _htmlUrl = "http://bitbucket.org/" + username + "/" + repository + "/src/" + mainBranch + "/" + filename;

                if (filepath.EndsWith("textile", StringComparison.Ordinal))
                    ContentText = markdownService.ConvertTextile(readme);
                else
                    ContentText = markdownService.ConvertMarkdown(readme);
            });
        }
    }
}
