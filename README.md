# tModLoader-Discord-Bot
A Discord bot written in C# using Discord.Net to serve the TMODLOADER Discord server. Uses .NET Core 2.0+
This bot is primarily written and designed to serve the TMODLOADER community.
TMODLOADER is a piece of software [available on steam](https://store.steampowered.com/app/1281930/tModLoader/) that allows you to play [Terraria](https://terraria.org/) with mods.

# Testing
To test the bot, you'll need a to provide the program with valid credentials. You'll need to test the bot on your own server. To do this, first visit the [Discord Developer Portal](https://discord.com/developers/applications) and click `New Application`. Provide your bot with a name, then click `Create`. Something like "My-tModLoader-bot" will work fine. Now, click `Bot` on the left, then click `Add Bot`, then click `Yes, do it!`. You'll now need to click the `Copy` button in the `Token` section. Next, we'll need to provide those credentials to our program. To do this, in the visual studio solution explorer, right click on `tModLoaderDiscordBot` and click `Properties`. In the window that appears, click `Debug`. Now, click the `Add` button in the `Environment variables` section. Type in `TestBotToken` for the name and paste in your copied bot token into the value column. 

Next, we need to invite the bot to a server you have admin permissions in. Follow the instructions in [here](https://github.com/jagrosh/MusicBot/wiki/Adding-Your-Bot-To-Your-Server), except before clicking `copy` first click `Administrator` in the `Bot Permissions` section that appeared.

Now, the bot is registered to the server and the bot credentials are in the project. Now, make sure the solution configuration is set to `Debug - Test Bot` and you are good to debug. When you debug, you should see the bot appear in your server after it has finished initalizing, once that is done you can test your code as usual.

Make sure to set the token in an environment variable called 'TestBotToken' or change it in Program.cs

# Can I use this bot?
No. This bot was specifically made for our TMODLOADER server. 
If you wish to have a bot with certain features this bot has, the best thing you can do is join our Discord server and contact one of the developers.

# License
The default license is 'all-rights reserved'. This means, all rights to this work are reserved to its author(s). In this case, that's me, Jofairden, and any other contributors. I made this bot, it is my code. The fact that it is open-source does not mean you can take my code and claim it as your own. Again, feel free to learn from it, but please be so polite to not steal it.


# Technical details
This bot is written in C# using .NET Core and the Discord.Net library.
.NET Core runs natively on linux, this allows the bot to run as 24/7 service for our server.
The Discord.Net library makes it easy to develop bots using C#, and provides many features that enhance the bot itself.
It also is built around asynchronous code design, which makes the bot itself more responsive.

# Functions
Our bot's feaures are specifically designed for the TMODLOADER Discord server.
These include but are not limited to:

1) A tag system. Tags be retrieved, edited and made global. Other users can also get global tags. Useful for storing information that is frequently given and otherwise needs to be typed.
2) Retrieve mod information that is uploaded to the mod browser.
3) Retrieve the status of certain websites important to modders. This would include our own website and also sites such as github, our documentation etc.
4) A permission system (grant user/role based permission for commands or modules).
5) A logging service offering flexibile logging options.
6) A configuration service to store settings and information of services provided by the bot.
7) A sticky role feature that allows remembering of roles even if a user leaves the server and comes back later. We use this for our softban role.
8) Anti-spam detection that will mute a user and delete their spam messages, and also will automatically kick them if they keep spammming.
9) A vote delete system that allows members to delete content they don't like to see.

# No database
This bot does not use a database. Here is why. Using a database adds a certain complexity to your application you may or may not want to deal with. For us, we haven't seen many use-cases to be using a database. It sure is fast, and probably much faster than our IO-Read/Writes, however speed of these operations is of no importance to us for now. Not using a database allows for easy data handling as IO operations are innate to the programming language itself, and making data copies (including time-based back-ups) is very easy and scalable for files.