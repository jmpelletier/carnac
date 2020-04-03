using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Carnac.Logic.Models;

namespace Carnac.Logic
{
    public class MessageProvider : IMessageProvider
    {
        readonly IShortcutProvider shortcutProvider;
        readonly IKeyProvider keyProvider;
        readonly PopupSettings settings;

        IgnoreMessages ignoreWindowsKey;
        IgnoreMessages userIgnores;

        public MessageProvider(IShortcutProvider shortcutProvider, IKeyProvider keyProvider, PopupSettings settings)
        {
            this.shortcutProvider = shortcutProvider;
            this.keyProvider = keyProvider;
            this.settings = settings;

            ignoreWindowsKey = new IgnoreMessages();
            ignoreWindowsKey.Add("Win");

            string keymapFolderPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Keymaps\";
            string ignoreFilePath = keymapFolderPath + "ignore.yml";

            if (Directory.Exists(keymapFolderPath) && File.Exists(ignoreFilePath))
            {
                string ymlText = File.ReadAllText(ignoreFilePath);
                userIgnores = IgnoreMessages.FromYml(ymlText);
            }
            else
            {
                userIgnores = new IgnoreMessages();
            }
        }

        public IObservable<Message> GetMessageStream()
        {
            /*
            shortcut Acc stream:
               - ! before item means HasCompletedValue is false
               - [1 & 2] means multiple messages are returned from get messages (1 and 2 in this case), others are a single message returned

            message merger:
               - * before items indicates the previous message has been modified (key has been merged into acc), otherwise new acc is created

            keystream   :  a---b---ctrl+r----ctrl+r----------ctrl+r----a--------------↓---↓
            shortcut Acc:  a---b---!ctrl+r---ctrl+r,ctrl+r---!ctrl+r---[ctrl+r & a]---↓---↓
            sel many    :  a---b-------------ctrl+r,ctrl+r-------------ctrl+r---a-----↓---↓
            msg merger  :  a---*ab-----------ctrl+r,ctrl+r-------------ctrl+r---a-----↓---*'↓ x2'
            */
            return keyProvider.GetKeyStream()
                .Scan(new ShortcutAccumulator(), (acc, key) => acc.ProcessKey(shortcutProvider, key))
                .Where(c => c.HasCompletedValue)
                .SelectMany(c => c.GetMessages())
                .Scan(new Message(), (acc, key) => Message.MergeIfNeeded(acc, key))
                .Where(m =>
                {
                    if (ignoreWindowsKey.MatchAnyKey(m) || userIgnores.MatchAllKeys(m))
                    {
                        return false;
                    }
                    if (settings.DetectShortcutsOnly && settings.ShowOnlyModifiers)
                    {
                        return m.IsShortcut && m.IsModifier;
                    }
                    if (settings.DetectShortcutsOnly)
                    {
                        return m.IsShortcut;
                    }
                    if (settings.ShowOnlyModifiers)
                    {
                        return m.IsModifier;
                    }
                    return true;
                });
        }
    }
}