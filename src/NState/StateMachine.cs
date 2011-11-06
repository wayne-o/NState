﻿namespace NState
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Transactions;
    using NBasicExtensionMethod;
    using Newtonsoft.Json;
    using NSure;

    /// <summary>
    ///   Enables specification of valid state changes to be applied to object
    ///   instances.
    /// </summary>
    [Serializable]
    public class StateMachine<TState> :
        IStateMachine<TState>
        where TState : State
    {
        private Dictionary<string, IStateMachine<TState>> _children = new Dictionary<string, IStateMachine<TState>>();

        public StateMachine() {} //for deserialization

        public StateMachine(string name,
                            IEnumerable<IStateTransition<TState>> stateTransitions,
                            TState startState,
                            IStateMachine<TState> parentStateMachine = null)
        {
            Ensure.That(stateTransitions.IsNotNull(), "stateTransitions not supplied.");

            Name = name;
            StateTransitions = stateTransitions;
            StartState = startState;
            Parent = parentStateMachine;
            if (parentStateMachine != null)
            {
                Parent.Children.Add(Name, this);
            }
            CurrentState = startState;
        }

        public string Name { get; set; }

        public IEnumerable<IStateTransition<TState>> StateTransitions { get; protected set; }

        public TState StartState { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public IStateMachine<TState> Parent { get; set; }

        public Dictionary<string, IStateMachine<TState>> Children
        {
            get { return _children; }
            set { _children = value; }
        }

        public TState CurrentState { get; set; }

        public Dictionary<DateTime, IStateTransition<TState>> History { get; set; }

        /// <summary>
        ///   NOTE 1: http://cs.hubfs.net/blogs/hell_is_other_languages/archive/2008/01/16/4565.aspx
        /// </summary>
        public void TriggerTransition(TState targetState,
                                      dynamic args = default(dynamic))
        {
            try
            {
                if (CurrentState != targetState) //make this explicit?
                {
                    var matches = StateTransitions.Where(t =>
                                                         t.StartStates.Where(s => s == CurrentState).Any() &&
                                                         t.EndStates.Where(e => e == targetState).Any());
                    if (matches.Any())
                    {
                        using (var t = new TransactionScope())
                            //this could be in-memory transactionalised using the memento pattern, or information could be sent to F# (see NOTE 1)
                        {
                            OnRaiseBeforeEveryTransition();
                            CurrentState.ExitFunction(args);
                            matches.First().TransitionFunction(targetState, this, args);
                            targetState.EntryFunction(args);
                            CurrentState = targetState;
                            OnRaiseAfterEveryTransition();
                            t.Complete();
                        }
                    }
                    else
                    {
                        if (Parent == null)
                        {
                            throw new Exception(); //to be caught below, refactor
                        }

                        Parent.TriggerTransition(targetState, args);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidStateTransitionException<TState>(CurrentState, targetState, innerException: e);
            }
        }

        public event EventHandler RaiseBeforeEveryTransitionEvent;

        public event EventHandler RaiseAfterEveryTransitionEvent;

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnRaiseBeforeEveryTransition()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = RaiseBeforeEveryTransitionEvent;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                // Format the string to send inside the CustomEventArgs parameter
                //e.Message += String.Format(" at {0}", DateTime.Now.ToString());

                // Use the () operator to raise the event.
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnRaiseAfterEveryTransition()
        {
            var handler = RaiseAfterEveryTransitionEvent;

            if (handler != null) // Event will be null if there are no subscribers
            {
                handler(this, new EventArgs());
            }
        }

        public string SerializeToJsonDto()
        {
            var dto = StateMachineSerializationHelper.SerializeToDto(this, new ExpandoObject());
            var s = JsonConvert.SerializeObject(dto, Formatting.Indented,
                                                new JsonSerializerSettings
                                                    {ObjectCreationHandling = ObjectCreationHandling.Replace});

            return s;
        }

        /// <summary>
        ///   Not in constructor because SM tree may not be 
        ///   completely initialized by constructor in current 
        ///   implementation.
        /// </summary>
        public IStateMachine<TState> InitializeWithJson(string json)
        {
            return StateMachineSerializationHelper.InitializeWithDto(this, JsonConvert.DeserializeObject
                                                                               (json));
        }
    }
}