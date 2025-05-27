# tModLoader-Discord-Bot
A Discord bot written in C# using the Discord.Net library to serve the tModLoader Discord server.

Built with .NET Core.

tModLoader is a piece of software [available on steam](https://store.steampowered.com/app/1281930/tModLoader/) that allows you to play [Terraria](https://terraria.org/) with mods.

## License
The default license is 'all-rights reserved'. This means, all rights to this work are reserved to its author(s). In this case, that's the tModLoader team contributors.

## Can I use this bot?
You can, but be advised to change the functionality to your needs.

The bot was written with single-server use in mind. 

That means some features may not be suitable for your server.

## Bot Features
Our bot's feaures are specifically designed for the tModLoader Discord server.
These include but are not limited to:

1) A tag system. Tags be retrieved, edited and made global. Other users can also retrieve global tags. Useful for storing information that is frequently given and otherwise needs to be typed.
2) Retrieve mod information that is uploaded to the mod browser.
3) Retrieve the status of certain websites important to modders. This would include our own website and also sites such as github, our documentation etc.
4) A permission system (grant user/role based permission for commands or modules).
5) A logging service offering flexible logging options.
6) A configuration service to store settings and information of services provided by the bot in JSON format.
7) A sticky role feature that allows remembering of roles even if a user leaves the server and comes back later. We use this feature primarily for our softban role.
8) Anti-spam detection that will mute a user and delete their spam messages, and also will automatically kick them if they keep spammming.
9) A vote delete system that allows members to delete content they don't like to see.

# Commands
The bot contains quite a lot of distinct features, checkout the Modules folder!

# Testing
To test the bot, you'll need a to provide the program with valid credentials. You'll need to test the bot on your own server. To do this, first visit the [Discord Developer Portal](https://discord.com/developers/applications) and click `New Application`. Provide your bot with a name, then click `Create`. Something like "My-tModLoader-bot" will work fine. Now, click `Bot` on the left, then click `Add Bot`, then click `Yes, do it!`. You'll now need to click the `Copy` button in the `Token` section. Next, we'll need to provide those credentials to our program. To do this, in the visual studio solution explorer, right click on `tModLoaderDiscordBot` and click `Properties`. In the window that appears, click `Open debug launch profiles UI` in the `Debug` section. Now, navigate to the `Environment variables` section. Type in `TestBotToken` for the name and paste in your copied bot token into the value column. Close that window.

We'll also need to do this with the `SteamWebAPIKey` retrieved from [Register Steam Web API Key](https://steamcommunity.com/dev/apikey).

Next, we need to invite the bot to a server you have admin permissions in. Follow the instructions in [here](https://github.com/jagrosh/MusicBot/wiki/Adding-Your-Bot-To-Your-Server), except before clicking `copy` first click `Administrator` in the `Bot Permissions` section that appeared.

Now, the bot is registered to the server and the bot credentials are in the project. Now, make sure the solution configuration is set to `Debug - Test Bot` and you are good to debug. When you debug, you should see the bot appear in your server after it has finished initializing, once that is done you can test your code as usual.

Make sure to set the token in an environment variable called 'TestBotToken'.

# Guild Configuration
The bot is able to handle mutliple Guild configurations at once. Each Guild get its own designated configuration, which will be stored to a JSON file locally.

If you are debugging the application, you can find it in the debug folder:

`..\<source root>\bin\Debug - Test Bot\netcoreapp3.1\data`

See the GuildConfig class in de Components folder for more info.

# Adding to Program.cas
If you intend to change `Program.cs` please use delegated / lazy properties. This helps us avoid unnecessary logic.
It is recommended to use a factory class if some class needs to be initialized. There are some examples in de Factories folder.
When making a factory class, give it a sensible "Create" function which will create and return the necessary object.