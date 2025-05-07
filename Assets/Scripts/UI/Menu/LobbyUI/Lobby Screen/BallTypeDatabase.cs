using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BallTypeDatabase", menuName = "Game Screen Data/Ball Logic/Ball Types Database")]
public class BallTypeDatabase : ScriptableObject
{
    [Serializable]
    public struct BallEntry
    {
        public GameEnumsUtil.BallType ballType;
        public GameObject prefab;
    }

    [SerializeField] private List<BallEntry> ballEntries = new();

    private Dictionary<GameEnumsUtil.BallType, GameObject> _lookup;

    public GameObject GetBallPrefab(GameEnumsUtil.BallType type)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<GameEnumsUtil.BallType, GameObject>();
            foreach (var entry in ballEntries)
            {
                if (!_lookup.ContainsKey(entry.ballType))
                {
                    _lookup.Add(entry.ballType, entry.prefab);
                }
            }
        }

        return _lookup.TryGetValue(type, out var prefab) ? prefab : null;
    }
}

