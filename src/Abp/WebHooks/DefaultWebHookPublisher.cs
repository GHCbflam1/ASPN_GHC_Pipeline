﻿using System;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Json;
using Abp.WebHooks.BackgroundWorker;

namespace Abp.WebHooks
{
    public class DefaultWebHookPublisher : AbpServiceBase, IWebHookPublisher, ITransientDependency
    {
        private readonly IGuidGenerator _guidGenerator;
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly IWebHookSubscriptionManager _webHookSubscriptionManager;
        private readonly IWebHooksConfiguration _webHooksConfiguration;

        public IWebHookStore WebHookStore { get; set; }

        public DefaultWebHookPublisher(
            IWebHookSubscriptionManager webHookSubscriptionManager,
            IWebHooksConfiguration webHooksConfiguration,
            IGuidGenerator guidGenerator,
            IBackgroundJobManager backgroundJobManager)
        {
            _guidGenerator = guidGenerator;
            _backgroundJobManager = backgroundJobManager;
            _webHookSubscriptionManager = webHookSubscriptionManager;
            _webHooksConfiguration = webHooksConfiguration;

            WebHookStore = NullWebHookStore.Instance;
        }

        [UnitOfWork]
        public virtual async Task PublishAsync(string webHookName, object data)
        {
            var webHook = await SaveWebHookAndGetAsync(webHookName, data);

            var subscriptions = await _webHookSubscriptionManager.GetAllSubscriptionsAsync(webHookName);

            foreach (var webHookSubscription in subscriptions)
            {
                await _backgroundJobManager.EnqueueAsync<WebHookSenderJob, WebHookSenderInput>(new WebHookSenderInput()
                {
                    WebHookId = webHook.Id,
                    Data = webHook.Data,
                    WebHookDefinition = webHook.WebHookDefinition,

                    WebHookSubscriptionId = webHookSubscription.Id,
                    Headers = webHookSubscription.Headers,
                    Secret = webHookSubscription.Secret,
                    WebHookUri = webHookSubscription.WebHookUri
                });
            }
        }

        [UnitOfWork]
        public virtual void Publish(string webHookName, object data)
        {
            var webHook = SaveWebHookAndGet(webHookName, data);

            var subscriptions = _webHookSubscriptionManager.GetAllSubscriptions(webHookName);

            foreach (var webHookSubscription in subscriptions)
            {
                _backgroundJobManager.Enqueue<WebHookSenderJob, WebHookSenderInput>(new WebHookSenderInput()
                {
                    WebHookId = webHook.Id,
                    Data = webHook.Data,
                    WebHookDefinition = webHook.WebHookDefinition,

                    WebHookSubscriptionId = webHookSubscription.Id,
                    Headers = webHookSubscription.Headers,
                    Secret = webHookSubscription.Secret,
                    WebHookUri = webHookSubscription.WebHookUri
                });
            }
        }

        protected virtual async Task<WebHookInfo> SaveWebHookAndGetAsync(string webHookName, object data)
        {
            var webHookInfo = new WebHookInfo()
            {
                Id = _guidGenerator.Create(),
                WebHookDefinition = webHookName,
                Data = _webHooksConfiguration.JsonSerializerSettings != null
                    ? data.ToJsonString(_webHooksConfiguration.JsonSerializerSettings)
                    : data.ToJsonString()
            };

            await WebHookStore.InsertAndGetIdAsync(webHookInfo);
            return webHookInfo;
        }

        protected virtual WebHookInfo SaveWebHookAndGet(string webHookName, object data)
        {
            var webHookInfo = new WebHookInfo()
            {
                Id = _guidGenerator.Create(),
                WebHookDefinition = webHookName,
                Data = _webHooksConfiguration.JsonSerializerSettings != null
                    ? data.ToJsonString(_webHooksConfiguration.JsonSerializerSettings)
                    : data.ToJsonString()
            };

            WebHookStore.InsertAndGetId(webHookInfo);
            return webHookInfo;
        }
    }
}