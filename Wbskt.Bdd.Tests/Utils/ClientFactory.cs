using Wbskt.Bdd.Tests.ContractWrappers;
using Wbskt.Client;
using Wbskt.Client.Configurations;

namespace Wbskt.Bdd.Tests.Utils;

public static class ClientFactory
{
    public static WbsktListener[] GetClientsForChannel(this ChannelSource channelSource)
    {
        var clients = new List<WbsktListener>();
        for (var i = 0; i < channelSource.NumberOfClients; i++)
        {
            var config = new WbsktConfigurationCustom
            {
                ChannelDetails = new ChannelDetails
                {
                    Secret = channelSource.ChannelSecret,
                    SubscriberId = channelSource.ChannelSubscriberId,
                },
                ClientDetails = new ClientDetails
                {
                    Name = $"cli.{i}.--.{Guid.NewGuid()}",
                    UniqueId = Guid.NewGuid()
                },
                WbsktServerAddress = Constants.WbsktServerAddress
            };

            clients.Add(new WbsktListener(config, null));
        }

        return clients.ToArray();
    }
}
