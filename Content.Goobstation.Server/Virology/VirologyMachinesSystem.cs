// SPDX-FileCopyrightText: 2026 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Virology;
using Content.Server._RedStar.Skills; // RS14
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Skills; // RS14
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction; // RS14
using Content.Shared.Paper;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Text;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Random; // RS14
using Robust.Shared.Utility;

namespace Content.Goobstation.Server.Virology;

public sealed partial class VirologyMachinesSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!; // RS14
    [Dependency] private readonly IRobustRandom _random = default!; // RS14

    private const float VirologyMachineMistakeChance = 0.6f; // RS14
    private static readonly ProtoId<SkillPrototype> VirologySkill = "Virology"; // RS14
    private readonly Dictionary<EntityUid, EntityUid> _pendingSwabUsers = new(); // RS14
    private readonly Dictionary<EntityUid, EntityUid> _unskilledRuns = new(); // RS14

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirologyMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VirologyMachineComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]); // RS14
        SubscribeLocalEvent<VirologyMachineComponent, EntInsertedIntoContainerMessage>(OnSwabInserted);
        SubscribeLocalEvent<VirologyMachineComponent, VirologyMachineCheckEvent>(OnAnalyzerCheck);
        SubscribeLocalEvent<VirologyMachineComponent, VirologyMachineDoneEvent>(OnMachineDone);
        SubscribeLocalEvent<VirologyMachineComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveVirologyMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var checkEv = new VirologyMachineCheckEvent();
            RaiseLocalEvent(uid, ref checkEv);
            if (checkEv.Cancelled)
            {
                SetAppearance(uid, false);
                var doneEv = new VirologyMachineDoneEvent(false);
                RaiseLocalEvent(uid, doneEv);
                continue;
            }

            if (!_power.IsPowered(uid))
            {
                SetAppearance(uid, false);
                comp.EndTime += TimeSpan.FromSeconds(frameTime);
                continue;
            }

            if (_timing.CurTime > comp.EndTime)
            {
                SetAppearance(uid, false);
                var doneEv = new VirologyMachineDoneEvent(true);
                RaiseLocalEvent(uid, doneEv);
                continue;
            }

            SetAppearance(uid, true);
        }
    }

    // RS14-start
    private void OnInteractUsing(Entity<VirologyMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<DiseaseSwabComponent>(args.Used))
            return;

        _pendingSwabUsers[args.Used] = args.User;
    }
    // RS14-end

    private void OnSwabInserted(Entity<VirologyMachineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != VirologyMachineComponent.SwabSlotId)
            return;

        if (!TryComp<DiseaseSwabComponent>(args.Entity, out _))
            return;

        EnsureComp<ActiveVirologyMachineComponent>(ent, out var active);
        var audio = _audio.PlayPvs(ent.Comp.AnalysisSound, ent, AudioParams.Default.WithLoop(true).WithVariation(0.15f));
        if (audio.HasValue)
            ent.Comp.SoundEntity = audio.Value.Entity;
        // RS14-start
        if (_pendingSwabUsers.Remove(args.Entity, out var user) && !_skills.HasSkill(user, VirologySkill))
        {
            _unskilledRuns[ent.Owner] = user;
        }
        else
        {
            _unskilledRuns.Remove(ent.Owner);
        }
        // RS14-end
        active.EndTime = _timing.CurTime + ent.Comp.AnalysisDuration;
    }

    private void OnComponentInit(Entity<VirologyMachineComponent> ent, ref ComponentInit args)
    {
        if (_itemSlots.TryGetSlot(ent, VirologyMachineComponent.SwabSlotId, out var slot))
            ent.Comp.SwabSlot = slot;
        else
            _itemSlots.AddItemSlot(ent, VirologyMachineComponent.SwabSlotId, ent.Comp.SwabSlot);
    }

    private void OnAnalyzerCheck(Entity<VirologyMachineComponent> ent, ref VirologyMachineCheckEvent args)
    {
        args.Cancelled = !_itemSlots.TryGetSlot(ent, VirologyMachineComponent.SwabSlotId, out var slot) || slot.Item == null;
    }

    private void OnMachineDone(Entity<VirologyMachineComponent> ent, ref VirologyMachineDoneEvent args)
    {
        RemCompDeferred<ActiveVirologyMachineComponent>(ent);
        if (ent.Comp.SoundEntity != null)
        {
            _audio.Stop(ent.Comp.SoundEntity);
            ent.Comp.SoundEntity = null;
        }

        // RS14
        var wasUnskilledRun = _unskilledRuns.Remove(ent.Owner, out _);

        if (!args.Success
            || !_itemSlots.TryGetSlot(ent, VirologyMachineComponent.SwabSlotId, out var slot)
            || slot.Item == null)
            return;

        // RS14-start
        if (wasUnskilledRun && _random.Prob(VirologyMachineMistakeChance))
        {
            _itemSlots.TryEject(ent, ent.Comp.SwabSlot, null, out _);
            return;
        }
        // RS14-end

        if(!ent.Comp.Vaccinator)
            AnalyzeSwab(ent, (slot.Item.Value, null));
        else
            CreatePen(ent, (slot.Item.Value, null));
    }

    private void CreatePen(Entity<VirologyMachineComponent> ent, Entity<DiseaseSwabComponent?> swab)
    {
        // create a vaccine
        if (!Resolve(swab, ref swab.Comp) || !TryComp<DiseaseComponent>(swab.Comp.DiseaseUid, out var disease))
            return;

        var vaccine = Spawn(ent.Comp.VaccinePrototype, Transform(ent).Coordinates);
        if (!TryComp<DiseasePenComponent>(vaccine, out var vaccineComponent))
            return;

        if (swab.Comp.DiseaseUid != null)
        {
            vaccineComponent.Genotype = disease.Genotype;
            vaccineComponent.DiseaseUid = swab.Comp.DiseaseUid.Value;
        }

        _itemSlots.TryEject(ent, ent.Comp.SwabSlot, null, out _);
    }

    private void AnalyzeSwab(Entity<VirologyMachineComponent> ent, Entity<DiseaseSwabComponent?> swab)
    {
        if (!Resolve(swab, ref swab.Comp) || !TryComp<DiseaseComponent>(swab.Comp.DiseaseUid, out var disease))
            return;

        // build the report
        var report = new StringBuilder();
        report.AppendLine(Loc.GetString("disease-analyzer-report-title"));
        report.AppendLine(Loc.GetString("disease-analyzer-report-genotype", ("genotype", disease.Genotype)));
        report.AppendLine(Loc.GetString("disease-analyzer-report-type", ("type", Loc.GetString(_proto.Index(disease.DiseaseType).LocalizedName))));
        report.AppendLine(Loc.GetString("disease-analyzer-report-infection-rate", ("rate", disease.InfectionRate)));
        report.AppendLine(Loc.GetString("disease-analyzer-report-immunity-gain", ("rate", disease.ImmunityGainRate)));
        report.AppendLine(Loc.GetString("disease-analyzer-report-mutation-rate", ("rate", disease.MutationRate)));
        report.AppendLine(Loc.GetString("disease-analyzer-report-complexity", ("complexity", disease.Complexity)));

        report.AppendLine(Loc.GetString("disease-analyzer-report-effects-header"));
        foreach (var effectUid in disease.Effects.ContainedEntities)
        {
            var meta = MetaData(effectUid);
            if (TryComp<DiseaseEffectComponent>(effectUid, out var effectComp) && meta.EntityPrototype != null)
            {
                report.AppendLine(Loc.GetString("disease-analyzer-report-effect-line",
                    ("effect", Loc.GetString(meta.EntityPrototype.Name)),
                    ("description", Loc.GetString(meta.EntityPrototype.Description)),
                    ("severity", effectComp.Severity)));
            }
        }
        // print the report
        var printed = Spawn(ent.Comp.PaperPrototype, Transform(ent).Coordinates);
        _paper.SetContent((printed, EnsureComp<PaperComponent>(printed)), report.ToString());

        _itemSlots.TryEject(ent, ent.Comp.SwabSlot, null, out _);
        _audio.PlayPvs(ent.Comp.AnalyzedSound, ent);
    }

    private void SetAppearance(EntityUid uid, bool state)
    {
        _appearance.SetData(uid, DiseaseMachineVisuals.IsRunning, state);
    }

    private void AddAltVerb(Entity<VirologyMachineComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!ent.Comp.Vaccinator || HasComp<ActiveVirologyMachineComponent>(ent))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                ent.Comp.InjectorMode = !ent.Comp.InjectorMode;

                if (ent.Comp.InjectorMode)
                {
                    ent.Comp.VaccinePrototype = new EntProtoId("LiveInjector");
                    _popup.PopupEntity(Loc.GetString("vaccinator-toggle-live-injector"), ent);
                }
                else
                {
                    ent.Comp.VaccinePrototype = new EntProtoId("Vaccine");
                    _popup.PopupEntity(Loc.GetString("vaccinator-toggle-vaccine"), ent);
                }
            },
            Text = "Switch Mode",
            Priority = 25,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/dot.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }
}
