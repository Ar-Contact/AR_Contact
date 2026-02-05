using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AI'ýn sahip olabileceði tüm durumlarýn listesi
public enum AIStateId
{
    idle = 0,
    ChasePlayer = 1,
    Attack = 2
}

// Her AI durumunun (State) sahip olmasý gereken fonksiyonlarýn þablonu (Interface)
public interface AIState
{
    AIStateId GetId();
    void Enter(AiAgent agent);
    void Update(AiAgent agent);
    void Exit(AiAgent agent);
}