using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcherParametrs : WarriorParametrs
{
    void Awake()
    {
        RANGE_ATTACK = 300;
        maxHP = 150;
        hp = 150; 
    }
  
}
