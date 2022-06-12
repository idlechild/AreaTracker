# AreaTracker

- Designed for Super Metroid Area/Boss Randomizers.
- Instead of drawing lines between portals, it uses color and text descriptions at the portal locations.
- If your computer can run C# programs, then simply [download the MapData directory](https://github.com/idlechild/AreaTracker/raw/master/MapData.zip).
- The program should be intuitive to use, except perhaps for the area and boss names.
- You can change the names by renaming the MapData files.
- The All.conf file can also be used to adjust colors and other settings.

In case it is not intuitive to use:
- The larger areas are portals.  To connect portals, click two portals.
- You can reassign portals simply by creating a new connection, or double-click a portal to clear its connection.
- The smaller areas are bosses.  Click on the boss area to cycle through the bosses.
- If you want to change the boss order, reorder the bosses in the All.conf file.

If you want, you can change the layout further by editing the MapData text files.
There's no rule that all of the areas have to be square, or that portal areas must be larger than boss areas.

If all you want to do is make the entire layout larger or smaller, there is a backdoor method to do that.
Currently all of the areas are roughly aligned to a grid where each grid tile is 36x36.
The areas are often offset from the grid by a margin of 6.
With this in mind, if you run the AreaTracker from the command line and give it six parameters, it will modify all of the MapData text files.
The parameters are original margin, original grid tile size, X offset, Y offset, new grid tile size, new margin.
For example, if you want to make the entire map larger, you could try the command:
`AreaTracker 6 36 0 0 64 8` and then also adjust All.conf file (MapWidth 900 -> 1600, MapHeight 540 -> 960, MapMargin 6 -> 8).
This will put all of the modifications into a new folder. If you copy the AreaTracker.exe and All.conf files there, then you can try out the new configuration.
Another example, if you wanted to move everything down a couple of tiles, `AreaTracker 6 36 0 2 36 6`,
although if you have already made the map larger following the earlier example then it would be `AreaTracker 8 64 0 2 64 8`.
