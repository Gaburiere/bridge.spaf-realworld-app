﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bridge.Spaf;
using realworld.spaf.Models;
using realworld.spaf.Services;
using realworld.spaf.Services.impl;
using Retyped;
using static Retyped.knockout;

namespace realworld.spaf.ViewModels
{
    class HomeViewModel : LoadableViewModel
    {
        protected override string ElementId() => SpafApp.HomeId;

        private int _articlesInPage = 20;
        private string _actualTag = null;
        
        private readonly IArticleResources _resources;
        private readonly ISettings _settings;

        public KnockoutObservableArray<Article> Articles;
        public KnockoutObservableArray<Paginator> Pages;
        public KnockoutObservableArray<string> Tabs;
        public KnockoutObservableArray<string> Tags;
        public KnockoutObservable<int> ActiveTabIndex; 
        

        public HomeViewModel(IArticleResources resources, ISettings settings)
        {
            _resources = resources;
            _settings = settings;
            this.Articles = ko.observableArray.Self<Article>();
            this.Pages = ko.observableArray.Self<Paginator>();
            this.Tags = ko.observableArray.Self<string>();
            this.Tabs = ko.observableArray.Self<string>();
            this.ActiveTabIndex = ko.observable.Self<int>(-1);
        }

        public override async void OnLoad(Dictionary<string, object> parameters)
        {
            base.OnLoad(parameters);
            var loadArticle =
                this.LoadArticles(ArticleRequestBuilder.Default().WithLimit(this._settings.ArticleInPage));
            await Task.WhenAll(loadArticle,this.LoadTags());
            
            this.RefreshPaginator(loadArticle.Result);
        }

        /// <summary>
        /// Reset Tab
        /// </summary>
        /// <returns></returns>
        public async Task ResetTabs()
        {
            this.ActiveTabIndex.Self(-1);
            this.Tabs.removeAll();
            this._actualTag = null;
            var articleResponse = await this.LoadArticles(ArticleRequestBuilder.Default().WithLimit(this._settings.ArticleInPage));
            this.RefreshPaginator(articleResponse);
        }

        /// <summary>
        /// Go to page
        /// </summary>
        /// <param name="paginator"></param>
        /// <returns></returns>
        public async Task GoToPage(Paginator paginator)
        {
            this.Pages.Self().Single(s => s.Active.Self()).Active.Self(false);
            paginator.Active.Self(true);
            
            var request = ArticleRequestBuilder.Default()
                .WithOffSet((paginator.Page-1)*this._articlesInPage)
                .WithLimit(this._settings.ArticleInPage);

            if (!string.IsNullOrEmpty(this._actualTag))
                request = request.WithTag(this._actualTag);

            await this.LoadArticles(request);
        }

        /// <summary>
        /// Filter articles by tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task FilterByTag(string tag)
        {
            var tabName = $"#{tag}";
            await this.ArticlesForTab(tabName);
        }

        /// <summary>
        /// Load articles for passed tab
        /// </summary>
        /// <param name="tab"></param>
        /// <returns></returns>
        public async Task ArticlesForTab(string tab)
        {
            var tagName = tab.TrimStart('#');
            this._actualTag = tagName;

            var actualIndex = this.Tabs.Self().IndexOf(tab);
            
            if(actualIndex == -1)
                this.Tabs.push(tab);
            
            this.ActiveTabIndex.Self(this.Tabs.Self().IndexOf(tab));

            var articles = await this.LoadArticles(ArticleRequestBuilder.Default()
                .WithTag(tagName)
                .WithLimit(this._settings.ArticleInPage));
            this.RefreshPaginator(articles);
        }
        
        /// <summary>
        /// Load articles
        /// Clear list and reload
        /// </summary>
        /// <returns></returns>
        private async Task<ArticleResponse> LoadArticles(ArticleRequestBuilder request)
        {
            var articleResoResponse = await this._resources.GetArticles(request);
            this.Articles.removeAll();
            this.Articles.push(articleResoResponse.Articles);
            return articleResoResponse;
        }

        /// <summary>
        /// Reload tags
        /// </summary>
        /// <returns></returns>
        private async Task LoadTags()
        {
            var tags = await this._resources.GetTags();
            this.Tags.removeAll();
            this.Tags.push(tags.Tags);
        }
        
        /// <summary>
        /// When update articles rebuild paginator
        /// </summary>
        /// <param name="articleResoResponse"></param>
        private void RefreshPaginator(ArticleResponse articleResoResponse)
        {
            this._articlesInPage = articleResoResponse.Articles.Length;
            var pagesCount = (int) (articleResoResponse.ArticlesCount / this._articlesInPage);
            var range = Enumerable.Range(1, pagesCount);
            var pages = range.Select(s => new Paginator
            {
                Page = s
            }).ToArray();
            pages[0].Active.Self(true);
            this.Pages.removeAll();
            this.Pages.push(pages);
        }
    }
}
