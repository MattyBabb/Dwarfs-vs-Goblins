using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability
{
    private string name;
    private string description;
    private Sprite icon;
    private bool requiresTarget;
    private bool canCastOnSelf;
    private int cooldown;
    private GameObject particleEffect;
    private float areaOfEffect;

    public Ability(string aname, string adescription, Sprite aicon)
    {
        name = aname;
        icon = aicon;
        description = adescription;
    }

    public string AbilityName
    {
        get { return name; }
    }

    public string AbilityDescription
    {
        get { return description; }
    }

    public Sprite AbilityIcon
    {
        get { return icon; }
    }

    public int AbilityCooldown
    {
        get { return cooldown; }
    }

}
