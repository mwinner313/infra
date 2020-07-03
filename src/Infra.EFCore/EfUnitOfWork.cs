﻿using Domain;
using Infra.Eevents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Infra.EFCore
{
    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<EfUnitOfWork> _logger;

        public EfUnitOfWork(
            DbContext context,
            IEventBus eventBus,
            ILogger<EfUnitOfWork> logger)
        {
            _logger = logger;
            _eventBus = eventBus;

            Context = context;
        }

        public DbContext Context { get; }
        public IGenericRepository<T> GenericRepo<T>() where T : class => new EfGenericRepo<T>(Context);

        public async Task<int> Save(AggregateRoot root)
        {
            int rowCount;

            try
            {
                rowCount = await Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }

            foreach (var item in root.UncommittedChanges)
            {
                await DispatchEvents(item);
            }

            return rowCount;
        }

        private async Task DispatchEvents(Event item)
        {
            if (_eventBus == null)
            {
                return;
            }

            try
            {
                await _eventBus.Execute(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public Task<int> Save() => Context.SaveChangesAsync();
    }
}