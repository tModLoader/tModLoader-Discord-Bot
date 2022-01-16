# tModLoader-Discord-Bot
A Discord bot written in C# using Discord.Net to serve the tModLoader server. Uses .NET Core 2.0+

# Introductory
This bot is written in C# using .NET Core 2.0, with the Discord.Net library.
.NET Core runs natively on linux, this allows the bot to run as 24/7 service using a 1-XS x64 server.
The Discord.Net library makes it easy to develop bots using C#, and provides many features that enhance the bot itself. It also is built around asynchronous code design, which makes the bot itself _function better_ by design.

# Functions
This bot servers a few main purposes for our server. Users can:
1) Create tags. They tag be retrieved, edited and made global. Other users can also get global tags. Useful for storing information that is frequently given and otherwise needs to be typed.
2) Retrieve mod information. Any mod that is available on our browser our bot knows about. This is done by our ModService
3) Retrieve the status of certain websites important to modders. This would include our own website and also sites such as github, our documentation etc.

Our bot also features the following:
1) A permission system (grant user/role based permission for commands or modules)
2) A logging service offering flexibile logging options
3) A configuration service on guild-to-guild basis that allows various configurations to be done by guild owners or assigned administrator
4) A sticky role feature that allows remembering of roles even if a user leaves the server and comes back later
5) Anti-spam detection that will mute a user and delete their spam messages, and also will automatically kick them if they keep spammming
6) A vote delete system that allows members to delete content they don't like to see, which is fully configurable

# Exemplary
For sure this bot can be used as guidance on how to make a Discord bot, as well as how to utilize various Discord.Net features. However, keep in mind the license. Feel free to learn from this bot, but please do not blatantly copy-paste this code.

# Testing

To test the bot, you'll need a to provide the program with valid credentials. You'll need to test the bot on your own server. To do this, first visit the [Discord Developer Portal](https://discord.com/developers/applications) and click `New Application`. Provide your bot with a name, then click `Create`. Something like "My-tModLoader-bot" will work fine. Now, click `Bot` on the left, then click `Add Bot`, then click `Yes, do it!`. You'll now need to click the `Copy` button in the `Token` section. Next, we'll need to provide those credentials to our program. To do this, in the visual studio solution explorer, right click on `tModLoaderDiscordBot` and click `Properties`. In the window that appears, click `Debug`. Now, click the `Add` button in the `Environment variables` section. Type in `TestBotToken` for the name and paste in your copied bot token into the value column. 

Next, we need to invite the bot to a server you have admin permissions in. Follow the instructions in [here](https://github.com/jagrosh/MusicBot/wiki/Adding-Your-Bot-To-Your-Server), except before clicking `copy` first click `Administrator` in the `Bot Permissions` section that appeared.

Now, the bot is registered to the server and the bot credentials are in the project. Now, make sure the solution configuration is set to `Debug - Test Bot` and you are good to debug. When you debug, you should see the bot appear in your server after it has finished initalizing, once that is done you can test your code as usual.

# License
The default license is 'all-rights reserved'. This means, all rights to this work are reserved to its author(s). In this case, that's me, Jofairden. I made this bot, it is my code. The fact that it is open-source does not mean you can take my code and claim it as your own. Again, feel free to learn from it, but please be so polite to not steal it.

# No database
This bot does not use a database. Here is why. Using a database adds a certain complexity to your application you may or may not want to deal with. For us, we haven't seen many use-cases to be using a database. It sure is fast, and probably much faster than our IO-Read/Writes, however speed of these operations is of no importance to us for now. Not using a database allows for easy data handling as IO operations are innate to the programming language itself, and making data copies (including time-based back-ups) is very easy and scalable for files.

# Can I use this bot?
No. This bot was specifically made for our server. If you wish to have a bot with certain features this bot has, feel free to contact me.

# Learn more
To learn more about this bot, visit our wiki. It will cover in-depth guidance to how this bot operates.
