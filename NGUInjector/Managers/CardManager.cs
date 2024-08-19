using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NGUInjector.Managers
{
    public class CardManager
    {
        private readonly Character _character;
        private readonly int _maxManas;
        private CardsController _cardsController;
        private readonly IDictionary<cardBonus, float> _cardValues = new Dictionary<cardBonus, float>();

        public CardManager()
        {
            try
            {
                _character = Main.Character;
                _cardsController = _character.cardsController;
                _maxManas = _character.cardsController.maxManaGenSize();
                foreach (cardBonus bonus in Enum.GetValues(typeof(cardBonus)))
                {
                    float bonusValue = _cardsController.generateCardEffect(bonus, 6, 1, 1, false);
                    _cardValues.Add(bonus, bonusValue);
                }
            }
            catch (Exception e)
            {
                Main.Log(e.Message);
                Main.Log(e.StackTrace);
            }
        }
        int CurTogg() => _character.cardsController.curManaToggleCount();
        public void CheckManas()
        {

            try
            {
                List<Mana> manas = _character.cards.manas;
                int lowestCount = int.MaxValue;
                Mana lowestProgress = manas[0];
                bool balanceMayo = true;
                if (_character.cards.cards.Count > 0)
                {
                    Card card = _character.cards.cards[0];
                    for (int i = 0; i < manas.Count; i++)
                    {
                        if (manas[i].amount < card.manaCosts[i])
                        {
                            balanceMayo = false;
                            break;
                        }
                    }
                    for (int i = 0; i < manas.Count && !balanceMayo; i++)
                    {
                        if (manas[i].amount >= card.manaCosts[i] && manas[i].running)
                        {
                            _cardsController.toggleManaGen(i);
                            i = 0;
                        }
                        if (manas[i].amount < card.manaCosts[i] && !manas[i].running && CurTogg() < _maxManas) _cardsController.toggleManaGen(i);
                    }
                }

                if (balanceMayo)
                {
                    foreach (Mana mana in manas)
                    {

                        if (lowestCount > mana.amount)
                        {
                            lowestCount = mana.amount;
                            lowestProgress = mana;
                        }
                        else if (lowestCount == mana.amount && mana.progress < lowestProgress.progress) lowestProgress = mana;
                    }

                    for (int i = 0; i < manas.Count; i++)
                    {
                        int toggStart = CurTogg();
                        float progressPerSec = _cardsController.manaGenProgressPerTick() * 50;
                        Mana mana = manas[i];

                        if (mana.running)
                        {
                            if (mana.amount - lowestCount == 1 && Math.Abs((1f + mana.progress - lowestProgress.progress) / progressPerSec) <= 10) continue;
                            else if (mana.amount == lowestCount && Math.Abs((mana.progress - lowestProgress.progress) / progressPerSec) <= 10) continue;
                        }
                        else
                        {
                            if (CurTogg() >= _maxManas) continue;
                            else if (mana.amount == lowestCount && Math.Abs((mana.progress - lowestProgress.progress) / progressPerSec) > 10) continue;
                            else if (mana.amount - lowestCount == 1 && Math.Abs((1f + mana.progress - lowestProgress.progress) / progressPerSec) > 10) continue;
                            else if (mana.amount - lowestProgress.amount > 1) continue;
                        }
                        _cardsController.toggleManaGen(i);
                        if (toggStart < CurTogg()) i = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Main.Log(e.Message);
                Main.Log(e.StackTrace);
            }
        }

        public void TrashCard(int index)
        {
            if (_character.cards.cards[index].isProtected)
            {
                _character.cards.cards[index].isProtected = false;
            }
            _cardsController.trashCard(index);
        }

        public void TrashCards()
        {
            try
            {
                if (Main.Settings.TrashCards)
                {
                    var cards = _character.cards.cards;
                    if (cards.Count > 0)
                    {
                        int index = (cards.Count - 1);
                        while (index >= 0)
                        {
                            Card card = cards[index];

                            //Don't trash adventure cards unless the trash adventure flag is set
                            if (card.bonusType == cardBonus.adventureStat && !Main.Settings.TrashAdventureCards)
                            {
                                index--;
                                continue;
                            }

                            //Don't trash protected cards unless the trash protected flag is set or its chonker card with the trash chonker flag set
                            if (card.isProtected && !Main.Settings.TrashProtectedCards && (card.cardRarity != rarity.BigChonker || !Main.Settings.TrashChunkers))
                            {
                                index--;
                                continue;
                            }

                            if (card.type == cardType.end)
                            {
                                if (_character.inventory.inventory.Any(c => c.id == 492))
                                {
                                    Main.LogCard($"Trashed Card: Bonus Type: END, due to already having the END piece");
                                    TrashCard(index);
                                }
                                else if (cards.Count(c => c.type == cardType.end) > 1)
                                {
                                    Main.LogCard($"Trashed Card: Bonus Type: END, due to already having one in the cards list");
                                    TrashCard(index);
                                }
                            }
                            else if ((int)cards[index].cardRarity <= Main.Settings.CardsTrashQuality - 1)
                            {
                                Main.LogCard($"Trashed Card: Cost: {card.manaCosts.Sum()} Rarity: {card.cardRarity} Bonus Type: {card.bonusType}, due to Quality settings");
                                TrashCard(index);
                            }
                            else if (cards[index].manaCosts.Sum() <= Main.Settings.TrashCardCost)
                            {
                                Main.LogCard($"Trashed Card: Cost: {card.manaCosts.Sum()} Rarity: {card.cardRarity} Bonus Type: {card.bonusType}, due to Cost settings");
                                TrashCard(index);
                            }
                            else if (Main.Settings.DontCastCardType.Contains(card.bonusType.ToString()))
                            {
                                //Dont trash cards of BigChonker rarity unless the flag is set
                                if (card.cardRarity != rarity.BigChonker || Main.Settings.TrashChunkers)
                                {
                                    Main.LogCard($"Trashed Card: Cost: {card.manaCosts.Sum()} Rarity: {card.cardRarity} Bonus Type: {card.bonusType}, due to trash all settings");
                                    TrashCard(index);
                                }
                            }

                            index--;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Main.Log(e.Message);
                Main.Log(e.StackTrace);
            }
        }

        public void CastCards()
        {
            bool castCard = true;
            while (castCard && _character.cards.cards.Count > 0)
            {
                Card card = _character.cards.cards[0];
                List<Mana> manas = _character.cards.manas;
                if (Main.Settings.TrashCards) TrashCards(); //Make sure all cards in inventory are ones that should be cast
                for (int i = 0; i < card.manaCosts.Count; i++)
                {
                    //Main.LogCard(manas[i].amount.ToString());
                    //Main.LogCard(card.manaCosts[i].ToString());
                    if (manas[i].amount < card.manaCosts[i])
                    {
                        //Main.LogCard("false");
                        castCard = false;
                        break;
                    }
                }
                //Main.LogCard(castCard.ToString());
                if (castCard)
                {
                    Main.LogCard($"Cast Card: Cost: {card.manaCosts.Sum()} Rarity: {card.cardRarity} Bonus Type: {card.bonusType}");
                    if (card.isProtected) card.isProtected = false;
                    _cardsController.tryConsumeCard(0);
                }
            }
        }

        public void SortCards()
        {
            try
            {
                _character.cards.cards.Sort(CompareCards);
                for (var i = 0; i < _character.cards.cards.Count; i++)
                {
                    _cardsController.updateDeckCard(i);
                }
            }
            catch (Exception e)
            {
                Main.Log(e.Message);
                Main.Log(e.StackTrace);
            }
        }

        private float getCardChange(Card card)
        {
            return (_cardsController.getBonus(card.bonusType) + card.effectAmount) / _cardsController.getBonus(card.bonusType);
        }

        private float getCardValue(Card card)
        {
            return (getCardChange(card) - 1) / card.manaCosts.Sum();
        }

        private float getCardNormalValue(Card card)
        {
            return getCardValue(card) / _cardValues[card.bonusType];
        }

        private int CompareByPriority(string priority, Card c1, Card c2)
        {
            string[] temp = priority.Split(':');
            string bonusType = temp.Length > 1 ? temp[1] : "";
            temp[0] = temp[0].ToUpper();
            bool sortAsc = temp[0].EndsWith("-ASC");
            if (sortAsc)
            {
                temp[0] = temp[0].Substring(0, temp[0].Length - 4);
            }
            priority = temp[0];

            if (priority == "RARITY")
            {
                //return c2.cardRarity.CompareTo(c1.cardRarity);
                return DirectionalCompareTo(c1.cardRarity, c2.cardRarity, sortAsc);
            }

            if (priority == "TIER")
            {
                //return c2.tier.CompareTo(c1.tier);
                return DirectionalCompareTo(c1.tier, c2.tier, sortAsc);
            }

            if (priority == "COST")
            {
                //return c2.manaCosts.Sum().CompareTo(c1.manaCosts.Sum());
                return DirectionalCompareTo(c1.manaCosts.Sum(), c2.manaCosts.Sum(), sortAsc);
            }

            if (priority == "TYPE")
            {
                if (string.IsNullOrWhiteSpace(bonusType))
                {
                    return 0;
                }

                //if (c1.bonusType.ToString() == c2.bonusType.ToString())
                //    return 0;
                //else if (c2.bonusType.ToString() == bonusType)
                //    return 1;
                //else if (c1.bonusType.ToString() == bonusType)
                //    return -1;
                //else
                //    return 0;

                if (c1.bonusType.ToString() == c2.bonusType.ToString())
                    return 0;
                else if (c1.bonusType.ToString() == bonusType)
                    return sortAsc ? 1 : -1;
                else if (c2.bonusType.ToString() == bonusType)
                    return sortAsc ? -1 : 1;
                else
                    return 0;
            }

            if (priority == "PROTECTED")
            {
                //return c2.isProtected.CompareTo(c1.isProtected);
                return DirectionalCompareTo(c1.isProtected, c2.isProtected, sortAsc);
            }

            if (priority == "CHANGE")
            {
                //return getCardChange(c2).CompareTo(getCardChange(c1));
                return DirectionalCompareTo(getCardChange(c1), getCardChange(c2), sortAsc);
            }

            if (priority == "VALUE")
            {
                //return getCardValue(c2).CompareTo(getCardValue(c1));
                return DirectionalCompareTo(getCardValue(c1), getCardValue(c2), sortAsc);
            }

            if (priority == "NORMALVALUE")
            {
                if (c1.bonusType == c2.bonusType)
                {
                    //return getCardValue(c2).CompareTo(getCardValue(c1));
                    return DirectionalCompareTo(getCardValue(c1), getCardValue(c2), sortAsc);
                }
                //return getCardNormalValue(c2).CompareTo(getCardNormalValue(c1));
                return DirectionalCompareTo(getCardNormalValue(c1), getCardNormalValue(c2), sortAsc);
            }

            return 0;
        }

        private static int DirectionalCompareTo<T>(T value1, T value2, bool sortAsc) where T : IComparable
        {
            if (sortAsc)
            {
                return value1.CompareTo(value2);
            }
            else
            {
                return value2.CompareTo(value1);
            }
        }

        private int CompareCards(Card c1, Card c2)
        {
            foreach (var priority in Main.Settings.CardSortOrder)
            {
                var index = CompareByPriority(priority, c1, c2);

                if (index != 0) return index;
            }

            return 0;
        }
    }
}