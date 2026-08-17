using EncosyTower.Data;
using Game.Common;

namespace Game.Data
{
    [Data, DataWithoutId]
    public partial struct StatModifierData
    {
        public StatModifierData(
              CharacterStatKind stat
            , StatModifierOperation operation
            , float value
        ) : this()
        {
            _stat = stat;
            _operation = operation;
            _value = value;
        }

        [DataProperty] public readonly CharacterStatKind Stat => Get_Stat();

        [DataProperty] public readonly StatModifierOperation Operation => Get_Operation();

        [DataProperty] public readonly float Value => Get_Value();

        public readonly override string ToString()
            => $"{_stat} {_operation} {_value}";
    }
}
