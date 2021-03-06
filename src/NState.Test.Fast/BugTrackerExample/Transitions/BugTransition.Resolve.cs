using NState.Test.Fast.BugTrackerExample.TransitionActions;

namespace NState.Test.Fast.BugTrackerExample.Transitions
{
    public partial class BugTransition
    {
        public class Resolve : StateTransition<BugState, TransitionActionStatus>
        {
            public Resolve(BugTransitionAction.Resolve transitionAction)
                : base(transitionAction: transitionAction) { }

            public override BugState[] StartStates
            {
                get { return new[] { new BugState.Assigned(), }; }
            }

            public override BugState[] EndStates
            {
                get { return new[] { new BugState.Resolved(), }; }
            }
        }
    }
}