using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Microsoft.JSInterop;

namespace DisplatePlanner.Services;

public class JsInteropService(IJSRuntime jsRuntime) : IJsInteropService
{
    public async Task<Offset?> GetElementOffset(string elementId)
    {
        try
        {
            return await jsRuntime.InvokeAsync<Offset>("getElementOffset", elementId);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error getting element offset for element{elementId} with error message: {ex.Message}");
            return null;
        }
    }

    public async Task<ScrollData?> GetScrollPosition(string elementId)
    {
        try
        {
            return await jsRuntime.InvokeAsync<ScrollData>("getScrollPosition", elementId);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error getting scroll position for element{elementId} with error message: {ex.Message}");
            return null;
        }
    }

    public async Task SetScrollPosition(string elementId, double scrollLeft, double scrollTop)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("setScrollPosition", elementId, scrollLeft, scrollTop);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error setting scroll position for element: {elementId} with error message: {ex.Message}");
        }
    }

    public async Task AddZoomPreventingHandler(string elementId, string eventName)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("myUtils.addZoomPreventingHandler", elementId, eventName);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error preventing event: {eventName} for element: {elementId} with error message: {ex.Message}");
        }
    }
}