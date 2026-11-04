using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;

namespace TaskFlow.Api.IntegrationTests;

public class UnitOfWorkTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ExecuteInTransaction_CommitsSavedChangesAndReturnsResult()
    {
        var user = CreateUser();
        using var cancellation = new CancellationTokenSource();
        await using var context = CreateContext();
        IUnitOfWork unitOfWork = context;

        var result = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            Assert.Equal(cancellation.Token, token);
            Assert.NotNull(context.Database.CurrentTransaction);
            Assert.Equal(IsolationLevel.ReadCommitted,
                context.Database.CurrentTransaction.GetDbTransaction().IsolationLevel);
            context.Users.Add(user);
            await unitOfWork.SaveChangesAsync(token);
            return user.Id;
        }, cancellation.Token);

        Assert.Equal(user.Id, result);
        Assert.Null(context.Database.CurrentTransaction);
        await using var verification = CreateContext();
        var stored = await verification.Users.SingleAsync(item => item.Id == result);
        Assert.Equal(user.KeycloakSub, stored.KeycloakSub);
    }

    [Fact]
    public async Task ExecuteInTransaction_ExceptionAfterSave_RollsBackSavedChanges()
    {
        var user = CreateUser();
        await using var context = CreateContext();
        IUnitOfWork unitOfWork = context;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.ExecuteInTransactionAsync<Guid>(async token =>
            {
                context.Users.Add(user);
                await unitOfWork.SaveChangesAsync(token);
                Assert.True(await context.Users.AnyAsync(item => item.Id == user.Id, token));
                throw new InvalidOperationException("Fallo posterior al guardado.");
            }));

        Assert.Equal("Fallo posterior al guardado.", exception.Message);
        Assert.Null(context.Database.CurrentTransaction);
        await using var verification = CreateContext();
        Assert.False(await verification.Users.AnyAsync(item => item.Id == user.Id));
    }

    [Fact]
    public async Task ExecuteInTransaction_CancellationAfterSave_RollsBackSavedChanges()
    {
        var user = CreateUser();
        using var cancellation = new CancellationTokenSource();
        await using var context = CreateContext();
        IUnitOfWork unitOfWork = context;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                context.Users.Add(user);
                await unitOfWork.SaveChangesAsync(token);
                Assert.True(await context.Users.AnyAsync(item => item.Id == user.Id, token));
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return user.Id;
            }, cancellation.Token));

        Assert.Null(context.Database.CurrentTransaction);
        await using var verification = CreateContext();
        Assert.False(await verification.Users.AnyAsync(item => item.Id == user.Id));
    }

    [Fact]
    public async Task ExecuteInTransaction_WithoutSave_DoesNotPersistTrackedChanges()
    {
        var user = CreateUser();
        await using var context = CreateContext();
        IUnitOfWork unitOfWork = context;

        var result = await unitOfWork.ExecuteInTransactionAsync(token =>
        {
            context.Users.Add(user);
            return Task.FromResult(user.Id);
        });

        Assert.Equal(user.Id, result);
        Assert.Null(context.Database.CurrentTransaction);
        await using var verification = CreateContext();
        Assert.False(await verification.Users.AnyAsync(item => item.Id == user.Id));
    }

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private static AppUser CreateUser() => new(Guid.NewGuid().ToString(), "Prueba de transacción", null);
}
