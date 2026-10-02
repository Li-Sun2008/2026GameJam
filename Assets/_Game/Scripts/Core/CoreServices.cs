using Spotlight.Contracts;
namespace Spotlight.Core
{
    public sealed class CoreServices
    {
        public SessionStateStore Store {get;private set;}
        public IStateTransactionService Transactions {get;private set;}
        public ICommandContextFactory Commands {get;private set;}
        public IBoardQuery Board {get;private set;}
        public IWorldQuery World {get;private set;}
        public IWorldMotionWriter Motion {get;private set;}
        public IResourceQuery Resources {get;private set;}
        public IEntityIdService Ids {get;private set;}
        public IRandomService Random {get;private set;}
        public IDeploymentService Deployment {get;private set;}
        public CoreServices(IGameCatalog catalog,IEventBus events){Store=new SessionStateStore(catalog);Transactions=new StateTransactionService(Store,events);Commands=new CommandContextFactory(Store);Board=new BoardQuery(Store);World=new WorldQuery(Store);Motion=new WorldMotionWriter(Store,events);Resources=new ResourceQuery(Store);Ids=new EntityIdService(Store);Random=new RandomService(Store);Deployment=new DeploymentService(Store,Transactions);}
    }
}
