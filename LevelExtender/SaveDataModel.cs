// File: SaveDataModel.cs
namespace LevelExtender
{
    /// <summary>
    /// A model class that holds all data that needs to be saved per-playthrough.
    /// </summary>
    public class SaveDataModel
    {
        public bool EnableWorldMonsters { get; set; } = false;

        // GROWTH:: add more per-save properties here in the future.
    }
}