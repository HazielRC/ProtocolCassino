using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.Required;
using BrokeProtocol.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtocolCassino
{
    public class Core : Plugin
    {
        public static Core Instance { get; private set; }
        public Configuration Configuration { get; set; } = new Configuration();

        public Core()
        {
            Instance = this;
            Info = new PluginInfo("Protocol Cassino", "pcs");
            Configuration.configuration();
        }
    }
    public class ManagerEventHandler : ManagerEvents
    {
        public override bool Start()
        {
            var entities = EntityCollections.Entities.Where(x => x.data == "casino.numberroulette");
            foreach(var entity in entities)
            {
                entity.svEntity.SvAddDynamicAction("onNumberRoulette", "Jugar");
            }
            return true;
        }
    }
    public class CustomEvents : IScript
    {
        public List<LabelID> options = new List<LabelID>()
        {
            new LabelID("&2$100", "100"),
            new LabelID("&2$200", "200"),
            new LabelID("&2$500", "500"),
            new LabelID("&2$1000", "1000"),
            new LabelID("&2$5000", "5000"),
            new LabelID("&2$10000", "10000"),
            new LabelID("&2$100000", "100000"),
            new LabelID("&2$1000000", "1000000"),
        };
        [CustomTarget]
        public void onNumberRoulette(ShEntity target, ShPlayer executor)
        {
            executor.svPlayer.SendOptionMenu("Number Roulette", executor.ID, "numberroulette", options.ToArray(), new LabelID[] { new LabelID("Play", "play") });
        }
    }

    public class PlayerEventsHandler : PlayerEvents
    {
        public override bool OptionAction(ShPlayer player, int targetID, string id, string optionID, string actionID)
        {
            if(id == "numberroulette")
            {
                var amount = int.Parse(optionID);
                var config = Core.Instance.Configuration.configuration();
                if (player.MyMoneyCount < amount)
                {
                    player.svPlayer.SendGameMessage(config.insuficientMoney);
                    return true;
                }
                player.TransferMoney(DeltaInv.RemoveFromMe, amount, true);
                Random rnd = new Random();
                int numAleatorio = rnd.Next(0, 10);
                var earn = (amount * config.winMultiplier) - amount;
                if (numAleatorio == config.winnerNumber)
                {
                    player.svPlayer.SendGameMessage(string.Format(config.winMessage, earn, numAleatorio));
                    player.TransferMoney(DeltaInv.AddToMe, amount * config.winMultiplier, true);
                    return true;
                } else
                {
                    player.svPlayer.SendGameMessage(string.Format(config.defeatMessage, earn, numAleatorio));
                }
            }
            return true;
        }
    }

    public class Configuration
    {
        private string configFolder = Path.Combine("Plugins", "ProtocolCassino");
        private string configFileName = Path.Combine("Plugins", "ProtocolCassino", "config.json");
        public Config configuration()
        {
            if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);

            if (!File.Exists(configFileName))
            {
                var model = new Config()
                {
                    winnerNumber = 10,
                    winMultiplier = 2,
                    winMessage = "You win! You earn ${0}, The number was {1}",
                    defeatMessage = "Uh, sorry you lose ${0}, The number was {1}",
                    insuficientMoney = "You don't have this ammount to play!"
                };
                File.WriteAllText(configFileName, JsonConvert.SerializeObject(model, Formatting.Indented));
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFileName));
        }
    }
    public class Config
    {
        public int winnerNumber { get; set; }
        public int winMultiplier { get; set; }
        public string winMessage { get; set; }
        public string defeatMessage { get; set; }
        public string insuficientMoney { get; set; }
    }
}
