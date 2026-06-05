using Content.Shared.Interaction;
using Content.Shared.Mech.Module.Components;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// Handles the insertion of mech module into mechs.
/// </summary>
public sealed class MechModuleSystem : MechInstallSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechModuleComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechModuleComponent, InsertModuleEvent>(OnInsert);
    }

    private void OnUsed(Entity<MechModuleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryPrepareInstall(args.User, mech, out var mechComp))
            return;

        if (mechComp == null)
            return;

        var used = 0;
        foreach (var module in mechComp.ModuleContainer.ContainedEntities)
        {
            used += TryComp<MechModuleComponent>(module, out var installedModule)
                ? installedModule.Size
                : 1;
        }

        if (used + ent.Comp.Size > mechComp.MaxModuleAmount)
        {
            Popup.PopupClient(Loc.GetString("mech-module-slot-full-popup"), args.User, args.User);
            return;
        }

        if (Whitelist.IsWhitelistFail(mechComp.ModuleWhitelist, ent.Owner))
        {
            Popup.PopupClient(Loc.GetString("mech-module-whitelist-fail-popup"), args.User, args.User);
            return;
        }

        if (HasDuplicateInstalled(ent.Owner, mechComp.ModuleContainer.ContainedEntities, args.User))
            return;

        StartInstallDoAfter(args.User, ent.Owner, mech, ent.Comp.InstallDuration, new InsertModuleEvent());
    }

    private void OnInsert(Entity<MechModuleComponent> ent, ref InsertModuleEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        var mech = args.Args.Target.Value;

        if (!TryPrepareInstall(args.Args.User, mech, out _))
            return;

        Mech.InsertEquipment(mech, ent.Owner, moduleComponent: ent.Comp);
        args.Handled = true;
    }
}
