# **_Player Driven Procedural Generation is a tool in which the user can make a 2D world to their taste and preferences._**

To generate a world, first make a biome map by drawing onto the first canvas where you would like biomes to be layed out and then hit generate. With this biome map you can now generate a full world map. Any steps of the generation that you want to change can be accessed in the value editor UI. For any complex changes such as Octaves, Persitance, and lacunarity, have a look into the wiki.

---

For the developer side of the tool, all sliders can be tweaked in what their min, max, and default values are. More biomes can be added by updating the biome enum and adding a new tile to the biome_palette in DrawCanvas. 

All canvas and biome map functionality is within WaveFunctionCollapse.cs, Cell.cs, DrawCanvas.cs, BiomePaletteInstance.cs, and WFCVisual.cs. 

All the world generation is in BGen.cs, Tunnel.cs, MapGenerator.cs, all representing it through MapVisual.cs, and ProgressBar.cs.

Any UI representation/interaction uses AutoHeight.cs, LayerViewerInstance.cs, NoiseField.cs, TunnelField.cs, and ValueEditor.cs.

All systems use functions and tools from Noise.cs, Enums.cs, Structs.cs, ProceduralUtilities.cs, and SaveSystem.cs.
