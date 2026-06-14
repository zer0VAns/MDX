using System.Text.Json;
using Microsoft.JSInterop;
using mdxEcommerce.Client.Models;

namespace mdxEcommerce.Client.Services;

public class CartService
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "mdx_cart";

    private List<CartItem> _items = [];
    private bool _isOpen = false;

    // Evento estático — se comparte entre todos los componentes
    public static event Action? OnCartChanged;

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public int     TotalCount => _items.Sum(i => i.Quantity);
    public decimal TotalPrice => _items.Sum(i => i.Subtotal);
    public bool    IsOpen     => _isOpen;

    public CartService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task LoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json))
                _items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
        }
        catch { _items = []; }

        OnCartChanged?.Invoke();
    }

    public void OpenCart()
    {
        _isOpen = true;
        OnCartChanged?.Invoke();
    }

    public void CloseCart()
    {
        _isOpen = false;
        OnCartChanged?.Invoke();
    }

    public async Task AddAsync(ProductDto product)
    {
        var existing = _items.FirstOrDefault(i => i.Id == product.Id);
        if (existing is not null)
            existing.Quantity++;
        else
            _items.Add(new CartItem
            {
                Id       = product.Id,
                Name     = product.Name,
                Price    = product.Price,
                ImageUrl = product.ImageUrl,
                Quantity = 1
            });

        await PersistAsync();
        OnCartChanged?.Invoke();
    }

    public async Task SetQuantityAsync(int productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.Id == productId);
        if (item is null) return;

        if (quantity <= 0)
            _items.Remove(item);
        else
            item.Quantity = quantity;

        await PersistAsync();
        OnCartChanged?.Invoke();
    }

    public async Task RemoveAsync(int productId)
    {
        _items.RemoveAll(i => i.Id == productId);
        await PersistAsync();
        OnCartChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        _items.Clear();
        await PersistAsync();
        OnCartChanged?.Invoke();
    }

    private async Task PersistAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_items);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch { }
    }
}