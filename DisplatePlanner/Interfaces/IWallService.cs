namespace DisplatePlanner.Interfaces;

public interface IWallService : IWallState, IWallCommands, IWallInteraction
{
    Task InitializeAsync();
    Task SwitchToNextRulerType();
}
