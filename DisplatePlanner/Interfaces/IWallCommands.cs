using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IWallCommands
{
    void AddPlate(PlateData selectedPlate, double x, double y);
    void RemoveSelectedPlates();
    void MoveLeft();
    void MoveRight();
    void MoveUp();
    void MoveDown();
    void RotateSelectedPlates();
    void ArrangePlatesInOneLine();
    void Copy();
    void Paste();
    void Swap();
    void Undo();
    void Redo();
    void ImportPlates(List<Plate> importedPlates);
}
