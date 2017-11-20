# TCGPlayer-MTG-Data-Scraper

Purpose: To programatically navigate TCGPlayer.com and pull pricing information on every Magic Card printed/that data exists for. This data is then inserted into a local MySQL Database for further manipulation in other programs.

Features: Selenium Web Driver, C#, MySQL

Expected Output: Likely will not work if the local MySQL Database connection does not exist/is not commented out. Otherwise, each card from every set is printed on a line to the console. This should be from Newer Sets to Older Sets, with the most expensive card printed first.

Expected Run Time: ~20 minutes


Please view TCGPlayer-MTG-Data-Scraper/TCGPlayerScraper/TCGPlayerScraper/MainWindow.xaml.cs for source code.