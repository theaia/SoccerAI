using System.Collections;
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
            case 6:
                return new Vector2(-0.707f, -0.707f); ;
            case 7:
                return Vector2.left;
            case 8:
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
    public static int GetForwardDirPerTeam(Team _team) {
        if (_team == Team.Home) {
            return 2;
        } else {
            return 6;
        }
    }

    public static RaycastHit2D GetDirectionInfo(Vector2 _origin, int _value) {
        RaycastHit2D _hit = Physics2D.CircleCast(_origin, .05f, IntDirToInput(_value), float.PositiveInfinity, ~0);
        return _hit;
    }

    public static int GetRandomCollisionWithTag(Vector2 _origin, string _tag, List<int> _dirs = null) {
        if (_dirs == null) {
            _dirs = GetAllDirections();
        }
        _dirs = RandomSortList(_dirs);

        foreach (int _dir in _dirs) {
            RaycastHit2D _hit = GetDirectionInfo(_origin, _dir);
            if (_hit.collider != null && _hit.collider.gameObject.CompareTag(_tag)) {
                return _dir;
            }
        }
        return -1; //Default return value if none found
	}

    public static int GetRandomCollisionWithoutTag(Vector2 _origin, string _tag, List<int> _dirs = null) {
        if (_dirs == null) {
            _dirs = GetAllDirections();
        }

        _dirs = RandomSortList(_dirs);

        foreach (int _dir in _dirs) {
            RaycastHit2D _hit = GetDirectionInfo(_origin, _dir);
            if (_hit.collider != null && !_hit.collider.gameObject.CompareTag(_tag)) {
                return _dir;
            }
        }
        return _dirs[Random.Range(0, _dirs.Count)]; //Default return value if none found
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

    public static Vector2 GetTeamBasedLocation(Vector2 _position, Team _team) {
        return _team == Team.Home ? new Vector2(Mathf.Abs(_position.x) * -1, _position.y) : new Vector2(Mathf.Abs(_position.x), _position.y);
    }
}
