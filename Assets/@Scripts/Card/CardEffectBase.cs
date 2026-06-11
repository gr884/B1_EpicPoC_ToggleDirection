using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 카드 효과의 추상 베이스. 효과 하나당 이 클래스를 상속한 SO 하나.
/// 새 효과 추가 = 이 클래스를 상속한 클래스 작성 + EffectType에 식별자 추가.
/// </summary>
public abstract class CardEffectBase : ScriptableObject
{
    /// <summary>
    /// 비주얼(CardEffectPlaySystem)·토템(TotemAuraSystem) 시스템이 효과를 식별하는 키.
    /// 실행 로직은 다형성(Apply)으로 처리하되, 이 두 시스템은 기존 enum 키를 그대로 사용한다.
    /// </summary>
    public abstract EffectType EffectTypeId { get; }

    /// <summary>효과 실행. ctx를 통해 체인 환경에 접근한다.</summary>
    public abstract IEnumerator Apply(EffectContext ctx);
}
