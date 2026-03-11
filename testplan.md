# Test Plan

When making significant changes to Inkscape Tile Maker, follow this procedure to ensure that nothing broke.

1. Build the project in Release mode and ensure there are no build errors.
1. Run the unit tests and ensure they all pass.
1. Open `TestsAndExamples/Layout_Text.svg`
	- Click `Open in External Editor` and make sure it opens in Inkscape.
	- In tileset options, `Fill Tiles` with default settings
	- Check that tiles have been added. They should be singular and one should be missing where image is empty.
	- Switch preview mode to `In Context` and ensure proper rendering.
	- Switch preview mode to `Paint` and test each tool, ensure that multiple tiles work together.
	- Disable grid, it should disappear, then re-enable it.
	- Check different zoom levels, at least one above 100% and one below. Ensure selection on canvas works as expected.
	- `File -> Save As...` Save a copy.
	- `File -> Open/New...` Open the copy in another window.
	- Ensure consistant tileset settings
	- Close all without saving.
1. Open `TestsAndExamples/DualGridWithSingular_Test.svg`
	- Switch preview mode to `Paint`
	- Select a white tile and paint.
	- Ensure correct dual tile visuals in paint.
	- Select the pink diamond tile and paint over the grid.
	- Return to image preview mode and select a white tile.
	- `Export -> Material` Export the material.
	- Ensure that material export contains a tileset image with all configurations of white tiles.
	- Close without saving
1. Create new with `Dual Grid Material` template
	- Clear tileset and ensure it is empty.
	- Fill tileset with default settings and ensure it fills correctly.
	- Configure the tileset as expected with changes for each tile.
	- Repeat paint tests.
	- `File -> Save` and reopen, ensure settings are preserved.
	- Repeat paint tests.