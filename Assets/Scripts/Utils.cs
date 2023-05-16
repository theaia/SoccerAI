using System.Collections.Generic;
using UnityEngine;

public static class Utils{
    public static Vector2 V2CenterPoint(this Vector2 v1, Vector2 v2) {
        return new Vector2((v1.x + v2.x) / 2f, (v1.y + v2.y) / 2f);
    }

    public static Vector2 V3CenterPoint(this Vector2 v1, Vector2 v2, Vector2 v3) {
        return new Vector2((v1.x + v2.x + v3.x) / 3f, (v1.y + v2.y + v3.y) / 3f);
    }

    public static Vector2 DirToClosestInput(Vector2 _dir) {
        Vector2 normalizedDirection = _dir.normalized; //Make sure the direction is normalized
        Vector2 closestDirection = Vector2.zero;

        closestDirection.x = Mathf.Round(normalizedDirection.x);
        closestDirection.y = Mathf.Round(normalizedDirection.y);

        closestDirection.x = Mathf.Clamp(closestDirection.x, -1, 1);
        closestDirection.y = Mathf.Clamp(closestDirection.y, -1, 1);

        return closestDirection;
    }

    public static int InputToDir(Vector2 _input) {
        //Debug.Log("Input to dir " + _input);
        if (_input == Vector2.up) {
            return 0;
        } else if (_input == new Vector2(0.707f, 0.707f) || _input == new Vector2(1f, 1f)) {
            return 1;
        } else if (_input == Vector2.right) {
            return 2;
        } else if (_input == new Vector2(0.707f, -0.707f) || _input == new Vector2(1f, -1f)) {
            return 3;
        } else if (_input == Vector2.down) {
            return 4;
        } else if (_input == new Vector2(-0.707f, -0.707f) || _input == new Vector2(-1f, -1f)) {
            return 5;
        } else if (_input == Vector2.left) {
            return 6;
        } else if (_input == new Vector2(-0.707f, 0.707f) || _input == new Vector2(-1f, 1f)) {
            return 7;
        } else {
            return -1;
		}
    }

    public static Vector2 IntDirToInput(int dir) {
        switch (dir) {
            default:
                return Vector2.zero;
            case 0:
                return Vector2.up;
            case 1:
                return new Vector2(0.707f, 0.707f);
            case 2:
                return Vector2.right;
            case 3:
                return new Vector2(0.707f, -0.707f); ;
            case 4:
                return Vector2.down;
            case 5:
                return new Vector2(-0.707f, -0.707f); ;
            case 6:
                return Vector2.left;
            case 7:
                return new Vector2(-0.707f, 0.707f); ;
        }
    }

    public static List<int> GetForwardDirsPerTeam(Team _team) {
        if(_team == Team.Home) {
            return new List<int>{ 1, 2, 3 };
		} else {
            return new List<int>{ 5, 6, 7 };
        }
    }

    public static List<int> GetBackwardDirsPerTeam(Team _team) {
        if (_team == Team.Home) {
            return new List<int> { 5, 6, 7 };
        } else {
            return new List<int> { 1, 2, 3 };
        }
    }

    public static int GetForwardDirPerTeam(Team _team) {
        if (_team == Team.Home) {
            return 2;
        } else {
            return 6;
        }
    }

    public static RaycastHit2D[] GetDirectionInfo(Vector2 _origin, int _value) {
        Vector2 _input = IntDirToInput(_value);
        //Debug.DrawRay(_origin, _input, Color.red);
        //Debug.DrawLine(_origin, _origin + (_input * 1f), Color.red, .1f);
        RaycastHit2D[] _hits = Physics2D.RaycastAll(_origin, _input, 1f, GameManager.Instance.DirectionLayersToCheck);
        return _hits;
    }

    public static int GetRandomCollisionWithTag(Vector2 _origin, string _tag, List<int> _dirs = null) {
        if (_dirs == null) {
            _dirs = GetAllDirections();
        }
        _dirs = RandomSortList(_dirs);

        foreach (int _dir in _dirs) {
            RaycastHit2D[] _hits = GetDirectionInfo(_origin, _dir);
            foreach (RaycastHit2D _hit in _hits) {
                if (_hit.collider != null && _hit.collider.gameObject.CompareTag(_tag)) {
                    return _dir;
                }
            }
        }
        return -1; //Default return value if none found
	}

    public static int GetDirWithoutTags(Player _player, string[] _perception, string[] _tags, DirectionType _dirType) {
        List<int> _dirs = GetDirs(_player, _dirType);
        //Debug.Log($"Randomly sorted list: {IntListToString(_dirs)}");
        
        for (int i = 0; i < _dirs.Count; i++) { //Loop through all dirs

            bool[] _matchingTag = new bool[_tags.Length];

            for (int w = 0; w < _tags.Length; w++) { //Check against every tag
                _matchingTag[w] = _perception[_dirs[i]] == _tags[w];
                //Debug.Log($"Looping {i} {w}");
                if (_matchingTag[w]) {
                    //Debug.Log($"{_tags[w]} found on loop {i} {w} in dir {_dirs[i]}. Breaking loop");
                    break;
                }

                if(w == _tags.Length - 1) {
                    //Debug.Log($"No tags found in direction {_dirs[i]}. Returning value");
                    return _dirs[i];

                }
            }
        }

        //Debug.Log($"Tags found in every direction");
        return -1; //Default return value if none found
    }

    public static int GetDirWithTags(Player _player, string[] _perception, string[] _tags, DirectionType _dirType) {
        List<int> _dirs = GetDirs(_player, _dirType);
        //Debug.Log($"Randomly sorted list: {IntListToString(_dirs)}");

        //Debug.Log($"dirs length {_dirs.Count}. tag length {_tags.Length}. perception length {_perception.Length}");
        for (int i = 0; i < _dirs.Count; i++) { //Loop through all dirs
            bool[] _matchingTag = new bool[_tags.Length];

            for (int w = 0; w < _tags.Length; w++) { //Check against every tag
                _matchingTag[w] = _perception[_dirs[i]] == _tags[w];
                //_newList.Add(_matchingTag[w] ? 1 : 0);
                if (_matchingTag[w]) {
                    //Debug.Log($"{_matchingTag[w]} found on loop {_tags[w]} in dir {_dirs[i]}. Returning value");
                    return _dirs[i];
                }
            }
        }

        //Debug.Log($"No tags found in any direction");
        return -1; //Default return value if none found
    }

    public static bool CheckDirClearOfTags(string[] _perception, string[] _tags, int _dir) {
        if(_dir < 0 || _dir > 8) {
            //Debug.Log($"Invalid dir input {_dir}.  Returning true");
            return true;
        }
        //Debug.Log($"dir {_dir}. tag length {_tags.Length}. perception length {_perception.Length}");
        bool[] _matchingTag = new bool[_tags.Length];
        //Debug.Log($"matching set with {_matchingTag.Length}.");
        for (int w = 0; w < _tags.Length; w++) { //Check against every tag
            _matchingTag[w] = _perception[_dir] == _tags[w];
            if (_matchingTag[w]) { 
                //Debug.Log(_tags[w] + " is making the current dir not clear");
                return false;
            }
        }

        /*for (int z = 0; z < _matchingTag.Length; z++) {
            if (_matchingTag[z] == true) {
                Debug.Log(_matchingTag[z] + " is making the current dir not clear");
                return false;
            }
        }*/
        //Debug.Log("Is free of objects to avoid");
        return true;
    }

    public static List<int> RandomSortList(List<int> inputList) {
        System.Random random = new System.Random();
        int count = inputList.Count;

        while (count > 1) {
            count--;
            int index = random.Next(count + 1);
            int temp = inputList[index];
            inputList[index] = inputList[count];
            inputList[count] = temp;
        }

        return inputList;
    }

    public static List<int> GetAllDirections() {
        return new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };
    }

    public static string GetOpposingPlayerTag(Player _player) {
        return _player.GetTeam() == Team.Home ? "away" : "home";
	}

    public static string GetTeamPlayerTag(Player _player) {
        return _player.GetTeam() == Team.Home ? "home" : "away";
    }

    public static string GetTeamGoalTag(Player _player) {
        return _player.GetTeam() == Team.Home ? "homegoal" : "awaygoal";
    }

    public static string GetOpponentGoalTag(Player _player) {
        return _player.GetTeam() == Team.Home ? "awaygoal" : "homegoal";
    }

    public static Vector2 GetDefendingZoneBasedLocation(Vector2 _position, Team _team) {
        return _team == Team.Home ? new Vector2(Mathf.Abs(_position.x) * -1, _position.y) : new Vector2(Mathf.Abs(_position.x), _position.y);
    }

    public static Vector2 GetAttackingZoneBasedLocation(Vector2 _position, Team _team) {
        return _team == Team.Away ? new Vector2(Mathf.Abs(_position.x) * -1, _position.y) : new Vector2(Mathf.Abs(_position.x), _position.y);
    }

    public static float GetDistanceToNearestPlayer(Player _currentPlayer, Team? _team) {
        float _tempClosestPlayerDistance = float.PositiveInfinity;
        List<Player> _playersToCheckAgainst = GameManager.Instance.GetPlayers(_team);

        foreach(Player _player in _playersToCheckAgainst) {
            if(_player != _currentPlayer) {
                float _currentDistance = Vector2.Distance(_currentPlayer.transform.position, _player.transform.position);
                if (_currentDistance < _tempClosestPlayerDistance) {
                    _tempClosestPlayerDistance = _currentDistance;
                }
            }
		}

        return _tempClosestPlayerDistance;
	}

    public static Player GetPlayerNearestBall(Team? _team = null) {
        Player _currentClosestPlayerToBall = GameManager.Instance.GetCachedPlayerNearestBall(_team);
        return _currentClosestPlayerToBall;
    }

    public static bool IsV2LocationInZone(Vector2 _value, Team _team) {
        if(_team == Team.Home) {
            return _value.x < 0;
        } else {
            return _value.x > 0;
        }
        
	}

    public static Vector2 GetRandomInput() {
        int _randomInput = Random.Range(0, 7);
        return IntDirToInput(_randomInput);
    }

    public static string IntArrayToString(int[] array) {
        if (array == null || array.Length == 0) {
            return "";
        }
        string result = array[0].ToString();
        for (int i = 1; i < array.Length; i++) {
            result += "," + array[i].ToString();
        }
        return result;
    }

    public static string IntListToString(List<int> list) {
        if (list == null || list.Count == 0) {
            return "";
        }
        string result = list[0].ToString();
        for (int i = 1; i < list.Count; i++) {
            result += "," + list[i].ToString();
        }
        return result;
    }

    public static List<int> GetDirs(Player _player, DirectionType _dirType) {
        List<int> _dirList = new List<int>();

        List<int> _forwardList = new List<int>();
        _forwardList = GetForwardDirsPerTeam(_player.GetTeam());
        RandomSortList(_forwardList);

        List<int> _neutral = new List<int>() { 0, 4 };
        RandomSortList(_neutral);

        List<int> _backward = new List<int>();
        _backward = GetBackwardDirsPerTeam(_player.GetTeam());
        RandomSortList(_backward);

        switch (_dirType) {
            case DirectionType.ForwardOnly:
                _dirList.AddRange(_forwardList);
                break;
            case DirectionType.ForwardPreferredNeutral:
                _dirList.AddRange(_forwardList);
                _dirList.AddRange(_neutral);
                break;
            case DirectionType.ForwardPreferred:
                _dirList.AddRange(_forwardList);
                _dirList.AddRange(_neutral);
                _dirList.AddRange(_backward);
                break;
            case DirectionType.BackwardOnly:
                _dirList.AddRange(_backward);
                break;
            case DirectionType.BackwardPreferredNeutral:
                _dirList.AddRange(_backward);
                _dirList.AddRange(_neutral);
                break;
            case DirectionType.BackwardPreferred:
                _dirList.AddRange(_backward);
                _dirList.AddRange(_neutral);
                _dirList.AddRange(_forwardList);
                break;
            case DirectionType.All:
                _dirList = GetAllDirections();
                RandomSortList(_dirList);
                break;
        }

        return _dirList;
    }

    public static void DebugDir(int _dir, Vector2 _origin) {
        if (_dir != -1) {
            Debug.DrawLine(_origin, _origin + IntDirToInput(_dir) * GameManager.Instance.PerceptionLength, Color.green, .1f);
        } else {
            DebugExtension.DebugPoint(_origin, Color.red, .1f, .1f);
        }
    }

    public static Vector2? Intersection(Vector2 pointA1, Vector2 dirA, Vector2 pointB1, Vector2 dirB) {
        // Line A represented as a1x + b1y = c1
        //Debug.DrawRay(pointA1, dirA, Color.white, 1f);
        //Debug.DrawRay(pointB1, dirB, Color.black, 1f);
        float a1 = dirA.y;
        float b1 = -dirA.x;
        float c1 = a1 * pointA1.x + b1 * pointA1.y;

        // Line B represented as a2x + b2y = c2
        float a2 = dirB.y;
        float b2 = -dirB.x;
        float c2 = a2 * pointB1.x + b2 * pointB1.y;

        float determinant = a1 * b2 - a2 * b1;

        // If the lines are parallel (det = 0), return null
        if (Mathf.Abs(determinant) < Mathf.Epsilon) // Using Epsilon for a tiny float number
        {
            return null;
        } else {
            // Otherwise, compute the intersection point
            float x = (b2 * c1 - b1 * c2) / determinant;
            float y = (a1 * c2 - a2 * c1) / determinant;
            return new Vector2(x, y);
        }
    }


    public static int GetForwardAngleFromBall(Player _player, float? _formationLine = null) {
        if(_player.GetTeam() == Team.Home) {
			if (_formationLine.HasValue) {
                return GameManager.Instance.GetBallLocation().y < _formationLine.Value ? 1 : 3;
			} else {
                return GameManager.Instance.GetBallLocation().y < 0 ? 1 : 3;
            }
		} else {
            if (_formationLine.HasValue) {
                return GameManager.Instance.GetBallLocation().y < _formationLine.Value ? 7 : 5;
            } else {
                return GameManager.Instance.GetBallLocation().y < 0 ? 7 : 5;
            }
        }

	}

    public static bool RandomBool() {
        return Random.value > 0.5f;
    }

    public static Vector2 ClampedArenaPos(Vector2 _value, float _sideMargins = 0, float _topMargins = 0) {
        float _newX = Mathf.Clamp(_value.x, GameManager.Instance.ArenaWidth.x + _sideMargins, GameManager.Instance.ArenaWidth.y - _sideMargins);
        float _newY = Mathf.Clamp(_value.y, GameManager.Instance.ArenaHeight.x + _topMargins, GameManager.Instance.ArenaHeight.y - _topMargins);
        return new Vector2(_newX, _newY);
	}

}

public enum DirectionType {
    ForwardOnly,
    ForwardPreferredNeutral,
    ForwardPreferred,
    BackwardOnly,
    BackwardPreferredNeutral,
    BackwardPreferred,
    All
}
