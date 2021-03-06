using System;

namespace NState.Test.Fast.BugTrackerExample.TransitionActions
{
    public partial class BugTransitionAction
    {
        public class Close : TransitionAction<BugState, TransitionActionStatus>
        {
            public override TransitionActionStatus Run(BugState targetState,
                                                       IStateMachine<BugState, 
                                                       TransitionActionStatus> stateMachine,
                                                       dynamic statefulObject, 
                                                       dynamic dto = null)
            {
                if (dto == null)
                {
                    throw new ArgumentNullException("dto");
                }

                if (dto.ClosedByName == null)
                {
                    throw new Exception("ClosedByName not supplied.");
                }

                statefulObject.StatefulObject.Bug.ClosedByName = dto.ClosedByName;

                return TransitionActionStatus.Success;
            }
        }
    }
}