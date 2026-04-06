using Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ManipulatorMiniGame : MonoBehaviour, IMiniGame
{
    public event Action MiniGameCompleted;

    public void StartGame()
    {
        throw new NotImplementedException();
    }

    public void StopGame()
    {
        throw new NotImplementedException();
    }
}

