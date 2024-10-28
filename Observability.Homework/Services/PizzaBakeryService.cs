using System.Collections.Concurrent;
using Observability.Homework.Exceptions;
using Observability.Homework.Models;

namespace Observability.Homework.Services;

public interface IPizzaBakeryService
{
    Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default);
}

public class PizzaBakeryService(ILogger<PizzaBakeryService> logger) : IPizzaBakeryService
{
    private readonly ConcurrentDictionary<Guid, Product> _bake = new();

    public async Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start cooking: {productId}", product.Id);

        try
        {
            await MakePizza(product, cancellationToken);
            await BakePizza(product, cancellationToken);
            await PackPizza(product, cancellationToken);

            logger.LogInformation("Finish cooking: {productId}", product.Id);

            return product;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Cancel cooking: {productId}", product.Id);

            DropPizza(product);
            throw;
        }
        catch (BurntPizzaException)
        {
            logger.LogError("Burnt product: {productId}", product.Id);

            return await DoPizza(product, cancellationToken);
        }
    }

    private async Task<Product> BakePizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bake pizza: {productId}", product.Id);

        PushToBake(product);
        var bakeForSeconds = new Random().Next(3, 9);
        await Task.Delay(TimeSpan.FromSeconds(bakeForSeconds), cancellationToken);
        if (bakeForSeconds > 7)
        {
            DropPizza(product);
            throw new BurntPizzaException("The pizza is burnt");
        }
        return PopFromBake(product);
    }

    private async Task<Product> MakePizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Make pizza: {productId}", product.Id);

        await Task.Delay(new Random().Next(1, 3) * 1000, cancellationToken);
        return product;
    }
    
    private async Task<Product> PackPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Pack pizza: {productId}", product.Id);

        await Task.Delay(new Random().Next(1, 2) * 1000, cancellationToken);
        return product;
    }

    private void PushToBake(Product product)
    {
        _bake[product.Id] = product;
    }

    private Product PopFromBake(Product product)
    {
        _bake.Remove(product.Id, out var pizza);
        return pizza!; //пусть у нас всегда есть пицца
    }
    
    private void DropPizza(Product product)
    {
        logger.LogInformation("Drop pizza: {productId}", product.Id);
        _bake.Remove(product.Id, out _);
    }
}