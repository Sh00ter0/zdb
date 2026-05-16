# `>_` Discord Panel Development Guide

This guide explains how to add new Discord control panels and how to extend existing panels in the current declarative panel framework.

For clarity, all examples use `MyNewPanel` as the working panel name. When creating a real panel, replace `MyNewPanel`, `my_new_panel`, and example entity names with your domain-specific names.

---
## `>_` Current Architecture

The panel flow is state-driven and declarative:

```text
Discord interaction
    -> GlobalInteractionModule
    -> InteractionDispatcher
    -> InteractionPipeline
    -> IPanelActionHandler
    -> IPanelViewState
    -> LayoutBuilder
    -> PanelLayout
    -> DiscordLayoutMapper
    -> LayoutMappers/*
    -> Discord.NET transport
```

The key rule:

```text
Handlers produce state.
Layout builders describe UI.
Renderers return layout.
Layout mappers translate layout into Discord.NET components.
```

---
## `>_` Main Folders

`Panels/Core`
: Framework contracts such as `IConfigPanel`, `IPanelActionHandler`, `IPanelViewState`, `RenderedPanel`, `PanelActionResult`, `InteractionDispatcher`, and `InteractionPipeline`.

`Panels/Core/Layout`
: Declarative UI model: `PanelLayout`, `ContainerComponent`, `TextComponent`, `ButtonComponent`, `SelectMenuComponent`, `ActionRowComponent`, `PanelActionDescriptor`.

`Panels/<panel_name>`
: The panel class, action handlers, payloads, and view states.

`Panels/LayoutBuilders`
: Shared layout-builder contracts plus panel-specific builder folders. Keep `IPanelLayoutBuilder` at this level, and put concrete builders under `Panels/LayoutBuilders/<PanelName>`.

`Panels/Rendering/<panel_name>`
: Thin view renderers. A renderer should only call the matching layout builder and return `RenderedPanel { Layout = ... }`.

`Panels/Rendering/LayoutMappers`
: Discord.NET infrastructure. This is where `ButtonBuilder`, `SelectMenuBuilder`, `ContainerBuilder`, and similar Discord.NET builders belong.

`Panels/Modals`
: Shared modal contracts plus panel-specific modal factory folders. Keep `IModalFactory` at this level, and put concrete factories under `Panels/Modals/<PanelName>`.

---

## `>_` How Actions Work

Discord component `customId` values are encoded by `IInteractionCodec` from `PanelInteraction`.

Example final action:

```csharp
new PanelActionDescriptor(
    Panel: "my_new_panel",
    Action: MyNewPanelActionIds.OpenDetails,
    EntityId: entityId.ToString())
```

`ButtonMapper` encodes `PanelActionDescriptor` into a Discord `customId`.

`SelectMenuMapper` supports two select menu styles:

- Navigation select menu: each `SelectMenuOptionComponent.Action` contains a final action. The option value becomes an encoded `PanelInteraction`.
- Data select menu: `SelectMenuComponent.Action` contains the handler action, and each `SelectMenuOptionComponent.Value` contains raw data such as `enabled`, `disabled`, or an ID.

`GlobalInteractionModule` detects select option values starting with `p:` and forwards that encoded interaction directly to the dispatcher. The dispatcher does not need to know whether the interaction came from a button, select menu, or modal.

---
## `>_` Adding A New Panel

This section uses `MyNewPanel` as the example panel.

### 1. Create The Folder Structure

```text
Application/Discord/Panels/MyNewPanel/
    MyNewPanel.cs
    Actions/
        MyNewPanelActionIds.cs
        BackToMyNewPanelOverviewAction.cs
        CloseMyNewPanelAction.cs
        OpenDetailsAction.cs
        SaveModeAction.cs
    Payloads/
        MyNewPanelPayloads.cs
    States/
        MyNewPanelOverviewState.cs
        MyNewPanelDetailsState.cs

Application/Discord/Panels/Rendering/MyNewPanel/
    MyNewPanelOverviewRenderer.cs
    MyNewPanelDetailsRenderer.cs

Application/Discord/Panels/LayoutBuilders/MyNewPanel/
    MyNewPanelOverviewLayoutBuilder.cs
    MyNewPanelDetailsLayoutBuilder.cs

Application/Discord/Panels/Modals/MyNewPanel/
    RenameMyNewPanelModalFactory.cs
```

Keep the domain-specific panel code under `Panels/MyNewPanel`. Keep concrete layout builders under `Panels/LayoutBuilders/MyNewPanel`. Keep concrete modal factories under `Panels/Modals/MyNewPanel`. Shared contracts such as `IPanelLayoutBuilder` and `IModalFactory` stay in their root folders.

### 2. Add View States

A state is a pure view model. It must not reference Discord.NET.

```csharp
using Application.Discord.Panels.Core;
using Domain.Entities;

namespace Application.Discord.Panels.MyNewPanel.States;

public sealed record MyNewPanelOverviewState : IPanelViewState
{
    public required MyEntity Entity { get; init; }
}
```

Add a separate state for each meaningful screen:

```csharp
public sealed record MyNewPanelDetailsState : IPanelViewState
{
    public required MyEntity Entity { get; init; }
    public required IReadOnlyList<MyDetail> Details { get; init; }
}
```

If an action only changes data and returns to an existing screen, reuse the existing state.

### 3. Add Action IDs

Keep action names in one place.

```csharp
namespace Application.Discord.Panels.MyNewPanel.Actions;

public static class MyNewPanelActionIds
{
    public const string Back = "@back";
    public const string ClosePanel = "close_panel";

    public const string OpenDetails = "open_details";
    public const string SaveMode = "save_mode";
    public const string OpenRename = "open_rename";
    public const string RenameSubmit = "rename_submit";
}
```

The action name is the contract between layout and handler. Do not translate it in middleware.

### 4. Add The Panel Class

The panel resolves handlers by `Action`. Do not add routing switches here.

```csharp
using Application.Discord.Panels.Core;
using Application.Discord.Panels.MyNewPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.MyNewPanel;

public sealed class MyNewPanel(
    IEnumerable<IPanelActionHandler> handlers,
    IMyEntityRepository repository) : ConfigPanel<IPanelViewState>
{
    public override string Id => "my_new_panel";

    public override async Task<IPanelViewState> BuildStateAsync(ConfigPanelContext context)
    {
        if (!long.TryParse(context.EntityId, out var entityId))
            throw new InvalidOperationException("Invalid entity ID.");

        var entity = await repository.GetByIdAsync(entityId)
            ?? throw new InvalidOperationException("Entity not found.");

        return new MyNewPanelOverviewState { Entity = entity };
    }

    public override async Task<PanelActionResult> ExecuteActionAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var handler = handlers.FirstOrDefault(x => x.Action == interaction.Action);

        if (handler == null)
            return new UpdatePanelResult { State = await BuildStateAsync(context) };

        return await handler.ExecuteAsync(context, interaction);
    }
}
```

### 5. Add Payloads

Payloads parse data from `ConfigPanelContext`: `EntityId`, `SubEntityId`, metadata, selected values, or modal values.

```csharp
using Application.Discord.Panels.Core;
using Application.Discord.Panels.Core.Payloads;

namespace Application.Discord.Panels.MyNewPanel.Payloads;

public sealed record MyNewPanelEntityPayload(long EntityId) : IInteractionPayload
{
    public static MyNewPanelEntityPayload FromContext(ConfigPanelContext ctx) =>
        new(long.Parse(ctx.EntityId ?? throw new InvalidOperationException("Missing EntityId.")));
}
```

For a data select menu:

```csharp
public sealed record MyNewPanelModePayload(long EntityId, string Mode) : IInteractionPayload
{
    public static MyNewPanelModePayload FromContext(ConfigPanelContext ctx)
    {
        var entityId = long.Parse(ctx.EntityId ?? throw new InvalidOperationException("Missing EntityId."));
        var mode = ctx.RawInteractionData?.FirstOrDefault()
            ?? throw new InvalidOperationException("Missing selected value.");

        return new MyNewPanelModePayload(entityId, mode);
    }
}
```

### 6. Add Action Handlers

Handlers execute application logic and return an intent: update panel, open modal, or close panel.

```csharp
using Application.Discord.Panels.Core;
using Application.Discord.Panels.MyNewPanel.Payloads;
using Application.Discord.Panels.MyNewPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.MyNewPanel.Actions;

public sealed class OpenDetailsAction(
    IMyEntityRepository entityRepository,
    IMyDetailRepository detailRepository) : IPanelActionHandler
{
    public string Action => MyNewPanelActionIds.OpenDetails;

    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = MyNewPanelEntityPayload.FromContext(context);

        var entity = await entityRepository.GetByIdAsync(payload.EntityId)
            ?? throw new InvalidOperationException("Entity not found.");

        var details = await detailRepository.GetAllByEntityIdAsync(payload.EntityId);

        return new UpdatePanelResult
        {
            State = new MyNewPanelDetailsState
            {
                Entity = entity,
                Details = details
            }
        };
    }
}
```

Back navigation is also a normal handler:

```csharp
public sealed class BackToMyNewPanelOverviewAction(IMyEntityRepository repository) : IPanelActionHandler
{
    public string Action => MyNewPanelActionIds.Back;

    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = MyNewPanelEntityPayload.FromContext(context);

        var entity = await repository.GetByIdAsync(payload.EntityId)
            ?? throw new InvalidOperationException("Entity not found.");

        return new UpdatePanelResult
        {
            State = new MyNewPanelOverviewState { Entity = entity }
        };
    }
}
```

Closing a panel is also a handler:

```csharp
public sealed class CloseMyNewPanelAction : IPanelActionHandler
{
    public string Action => MyNewPanelActionIds.ClosePanel;

    public Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        return Task.FromResult<PanelActionResult>(new ClosePanelResult());
    }
}
```

### 7. Add Layout Builders

The layout builder is where UI decisions live. It can inspect state and decide which components should be visible. It must not use Discord.NET.

```csharp
using Application.Discord.Panels.Core.Layout;
using Application.Discord.Panels.LayoutBuilders;
using Application.Discord.Panels.MyNewPanel.Actions;
using Application.Discord.Panels.MyNewPanel.States;
using Domain.Constants;

namespace Application.Discord.Panels.LayoutBuilders.MyNewPanel;

public sealed class MyNewPanelOverviewLayoutBuilder : IPanelLayoutBuilder<MyNewPanelOverviewState>
{
    public PanelLayout Build(MyNewPanelOverviewState state)
    {
        var entityId = state.Entity.Id.ToString();

        return new PanelLayout
        {
            Components =
            [
                new ContainerComponent(
                    Header: "Manage My New Panel",
                    AccentColor: AppColors.Info,
                    Components:
                    [
                        new SeparatorComponent(SeparatorSize.Large),
                        new TextComponent($"""
                            **Name:** `{state.Entity.Name}`
                            **Status:** `{state.Entity.Status}`
                            """),
                        new SeparatorComponent(SeparatorSize.Small, IsDivider: false),
                        new ActionRowComponent(
                        [
                            new ButtonComponent(
                                Label: "Details",
                                Action: new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.OpenDetails, entityId),
                                Style: ButtonStyleType.Primary),
                            new ButtonComponent(
                                Label: "Close",
                                Action: new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.ClosePanel, entityId),
                                Style: ButtonStyleType.Secondary)
                        ])
                    ])
            ]
        };
    }
}
```

Example details layout with an empty state:

```csharp
public sealed class MyNewPanelDetailsLayoutBuilder : IPanelLayoutBuilder<MyNewPanelDetailsState>
{
    public PanelLayout Build(MyNewPanelDetailsState state)
    {
        var body = state.Details.Count == 0
            ? "> *No details are currently configured.*"
            : string.Join("\n", state.Details.Select(x => $"- **{x.Name}** (`{x.Id}`)"));

        return new PanelLayout
        {
            Components =
            [
                new ContainerComponent(
                    Header: "Details",
                    AccentColor: AppColors.Info,
                    Components:
                    [
                        new SeparatorComponent(SeparatorSize.Large),
                        new TextComponent(body),
                        new SeparatorComponent(SeparatorSize.Small, IsDivider: false),
                        new ActionRowComponent(
                        [
                            new ButtonComponent(
                                Label: "Return",
                                Action: new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.Back, state.Entity.Id.ToString()),
                                Style: ButtonStyleType.Secondary)
                        ])
                    ])
            ]
        };
    }
}
```

Notice that the empty-state decision is in the layout builder, not in the renderer.

### 8. Add Renderers

Renderers should stay thin.

```csharp
using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders.MyNewPanel;
using Application.Discord.Panels.MyNewPanel.States;

namespace Application.Discord.Panels.Rendering.MyNewPanel;

public sealed class MyNewPanelOverviewRenderer(MyNewPanelOverviewLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    public bool CanRender(IPanelViewState state) => state is MyNewPanelOverviewState;

    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((MyNewPanelOverviewState)state)
        });
    }
}
```

Add one renderer per state:

```csharp
public sealed class MyNewPanelDetailsRenderer(MyNewPanelDetailsLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    public bool CanRender(IPanelViewState state) => state is MyNewPanelDetailsState;

    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((MyNewPanelDetailsState)state)
        });
    }
}
```

### 9. Register Everything In DI

Registrations currently live in `Client/Program.cs`, inside `AddApplicationInfrastructure`.

Add usings:

```csharp
using Application.Discord.Panels.MyNewPanel;
using Application.Discord.Panels.MyNewPanel.Actions;
using Application.Discord.Panels.LayoutBuilders.MyNewPanel;
using Application.Discord.Panels.Modals.MyNewPanel;
using Application.Discord.Panels.Rendering.MyNewPanel;
```

Add registrations:

```csharp
// --- PANELS ---
services.AddSingleton<IConfigPanel, MyNewPanel>();

// --- ACTION HANDLERS (MY NEW PANEL) ---
services.AddSingleton<IPanelActionHandler, OpenDetailsAction>();
services.AddSingleton<IPanelActionHandler, SaveModeAction>();
services.AddSingleton<IPanelActionHandler, BackToMyNewPanelOverviewAction>();
services.AddSingleton<IPanelActionHandler, CloseMyNewPanelAction>();

// --- LAYOUT BUILDERS ---
services.AddSingleton<MyNewPanelOverviewLayoutBuilder>();
services.AddSingleton<MyNewPanelDetailsLayoutBuilder>();

// --- RENDERERS ---
services.AddSingleton<IPanelViewRenderer, MyNewPanelOverviewRenderer>();
services.AddSingleton<IPanelViewRenderer, MyNewPanelDetailsRenderer>();

// --- MODAL FACTORIES ---
services.AddSingleton<IModalFactory, RenameMyNewPanelModalFactory>();
```

If the panel does not use modals, skip the modal folder and modal factory registration.

### 10. Open The Panel From A Slash Command

In the slash command controller, get the panel from `IPanelRegistry`, build the state, render it, and map the layout.

```csharp
var panel = panelRegistry.Get("my_new_panel");

var panelContext = new ConfigPanelContext
{
    Context = context,
    Services = serviceProvider,
    UserId = context.User.Id,
    EntityId = entity.Id.ToString(),
    RawInteractionData = null
};

var state = await panel.BuildStateAsync(panelContext);
var renderedPanel = await panelRenderer.RenderAsync(state);

var finalComponents = renderedPanel.Layout != null
    ? layoutMapper.Map(renderedPanel.Layout)
    : renderedPanel.Components;

await context.Interaction.FollowupAsync(
    text: renderedPanel.Content,
    embeds: renderedPanel.Embeds,
    components: finalComponents,
    ephemeral: true,
    flags: MessageFlags.ComponentsV2);
```

Use `MessageFlags.ComponentsV2` when sending panels that render V2 containers.

---
## `>_` Adding Modules To Existing Panels

A "module" means a new action, new screen, submenu, modal, or UI section inside an existing panel.

## Module Variant A: New Action Without A New Screen

Use this when the action performs work and returns to an existing view.

1. Add an action constant in the existing `<PanelName>ActionIds`.
2. Add an `IPanelActionHandler`.
3. In the handler, execute application logic and return `UpdatePanelResult` with an existing state.
4. Add a button or select menu option in the existing layout builder.
5. Register the handler in DI.

Example:

```csharp
public sealed class RefreshMyNewPanelAction(IMyEntityRepository repository) : IPanelActionHandler
{
    public string Action => MyNewPanelActionIds.Refresh;

    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = MyNewPanelEntityPayload.FromContext(context);
        var entity = await repository.RefreshAsync(payload.EntityId);

        return new UpdatePanelResult
        {
            State = new MyNewPanelOverviewState { Entity = entity },
            ToastMessage = "Panel refreshed."
        };
    }
}
```

## Module Variant B: New Screen In An Existing Panel

Use this when the module needs its own view.

1. Add a state in `States/`.
2. Add an action ID.
3. Add a handler that returns the new state.
4. Add a layout builder in `Panels/LayoutBuilders/<PanelName>`.
5. Add a renderer in `Rendering/<PanelName>`.
6. Add navigation to the existing layout builder.
7. Register the handler, layout builder, and renderer in DI.

Keep UI decisions in the layout builder. For example, `if(items.Count == 0)` belongs in the layout builder, not in the renderer.

## Module Variant C: Navigation Select Menu

Navigation select menus must place final actions directly in options:

```csharp
new SelectMenuComponent(
    Placeholder: "Choose action...",
    Action: new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.OpenDetails, entityId),
    Options:
    [
        new SelectMenuOptionComponent
        {
            Label = "Details",
            Description = "Open details",
            Action = new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.OpenDetails, entityId)
        },
        new SelectMenuOptionComponent
        {
            Label = "Rename",
            Description = "Open rename modal",
            Action = new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.OpenRename, entityId)
        }
    ])
```

There is no `menu_router`. The selected option already contains the final action.

## Module Variant D: Data Select Menu

Data select menus have one handler action and raw option values:

```csharp
new SelectMenuComponent(
    Placeholder: "Select mode...",
    Action: new PanelActionDescriptor("my_new_panel", MyNewPanelActionIds.SaveMode, entityId),
    Options:
    [
        new SelectMenuOptionComponent { Label = "Enabled", Value = "enabled" },
        new SelectMenuOptionComponent { Label = "Disabled", Value = "disabled" }
    ])
```

The handler reads the selected value from `context.RawInteractionData`.

## Module Variant E: New Modal

1. Add an action ID that opens the modal, for example `OpenRename`.
2. Add a handler that returns `OpenModalResult`.
3. Add an `IModalFactory` implementation in `Panels/Modals/<PanelName>`.
4. Add a submit action ID, for example `RenameSubmit`.
5. Add a submit handler.
6. Register the modal factory and handlers in DI.

Open-modal handler:

```csharp
public sealed class OpenRenameModalAction : IPanelActionHandler
{
    public string Action => MyNewPanelActionIds.OpenRename;

    public Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = MyNewPanelEntityPayload.FromContext(context);

        return Task.FromResult<PanelActionResult>(new OpenModalResult
        {
            ModalType = "RenameMyNewPanel",
            EntityId = payload.EntityId.ToString()
        });
    }
}
```

Modal factory:

```csharp
namespace Application.Discord.Panels.Modals.MyNewPanel;

public sealed class RenameMyNewPanelModalFactory(
    IDiscordUiService discordUiService,
    IInteractionCodec codec) : IModalFactory
{
    public bool CanCreate(string modalType) => modalType == "RenameMyNewPanel";

    public Modal Create(OpenModalResult result)
    {
        var modalId = codec.Encode(new PanelInteraction
        {
            Panel = "my_new_panel",
            Action = MyNewPanelActionIds.RenameSubmit,
            EntityId = result.EntityId
        });

        return discordUiService.CreateSingleInputModal(
            modalId,
            "Rename",
            "New name",
            "Enter new name...",
            50);
    }
}
```

The submit handler should read modal values through a payload from `ConfigPanelContext.Context.Interaction`.

## Module Variant F: New UI Section In An Existing Screen

If the module only adds static or state-derived UI to an existing screen, update only the matching layout builder.

Example:

```csharp
components.Add(new TextComponent($"""
    **Last sync:** `{state.Entity.LastSyncUtc}`
    """));
```

If the same UI fragment is reused across many builders, add a small helper near existing helpers, such as `ClientPanelLayout`.

## Adding A New Declarative Component Type

Only do this when the existing components are not enough.

1. Add a record in `Panels/Core/Layout/Components`, for example `ProgressComponent`.
2. Add a mapper in `Panels/Rendering/LayoutMappers`, for example `ProgressMapper`.
3. The mapper implements `ILayoutComponentMapper`.
4. Register the concrete mapper and the `ILayoutComponentMapper` binding in DI.
5. Use the new component only from layout builders.


```csharp
public sealed record ProgressComponent(int Current, int Total) : IUiComponent;

public sealed class ProgressMapper : ILayoutComponentMapper
{
    public bool CanMap(IUiComponent component) => component is ProgressComponent;

    public object Map(IUiComponent component)
    {
        var progress = (ProgressComponent)component;
        return new TextDisplayBuilder($"{progress.Current}/{progress.Total}");
    }
}
```
