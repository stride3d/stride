// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Stride.Core;
using Stride.Games;
using Stride.Input;

namespace Stride.Engine.InputInteractions;

public class InputInteractionSystem : GameSystemBase, IInputInteractionService
{
    private static Comparison<InputInteractionRequest> InteractionRequestComparer = (x, y) =>
    {
        int interactionTypeComparison = y.InteractionType.CompareTo(x.InteractionType);
        if (interactionTypeComparison != 0)
        {
            return interactionTypeComparison;
        }
        return y.Order.CompareTo(x.Order);
    };

    private readonly List<InputInteractionRequest> requests = [];

    private InputManager inputManager;

    private IInputInteraction activeInteraction;

    public bool HasActiveInteraction => activeInteraction is not null;

    public InputInteractionSystem([NotNull] IServiceRegistry registry)
        : base(registry)
    {
    }

    public override void Initialize()
    {
        base.Initialize();

        var inputSystem = Game?.GameSystems.FirstOrDefault(x => x is InputSystem) as InputSystem;
        inputManager = inputSystem?.Manager;

        UpdateOrder = (inputSystem?.UpdateOrder ?? InputSystem.DefaultUpdateOrder) + 1;

        Enabled = inputManager is not null;
        Visible = false;
    }

    public void Request(InputInteractionRequest request)
    {
        requests.Add(request);
    }

    public bool IsActiveInteraction(IInputInteraction interaction) => activeInteraction == interaction;

    public bool IsActiveInteractionOwner(object owner) => activeInteraction?.Owner == owner;

    public void ForceTerminateActiveInteraction()
    {
        activeInteraction?.Cancel();
        activeInteraction = null;
    }

    public override void Update(GameTime gameTime)
    {
        if (requests.Count > 0)
        {
            if (activeInteraction is null)
            {
                if (requests.Count > 1)
                {
                    requests.Sort(InteractionRequestComparer);
                }
                var nextRequest = requests[0];
                if (nextRequest != null)
                {
                    activeInteraction = nextRequest.Factory();

                    activeInteraction.Start();
                }
            }

            requests.Clear();   // All requests are rejected if there is an active interaction
        }

        if (activeInteraction is not null)
        {
            bool isStillRunning = activeInteraction.Update(gameTime);
            if (!isStillRunning)
            {
                activeInteraction.End();
                activeInteraction = null;
            }
        }
    }
}
