using NUnit.Framework;
using Scenes.Shop;

public class ShopControllerTests
{
    [Test]
    public void GetPointerTextForIndex_WhenSelected_ReturnsPointer()
    {
        Assert.That(ShopController.GetPointerTextForIndex(0, 0), Is.EqualTo("> "));
        Assert.That(ShopController.GetPointerTextForIndex(2, 2), Is.EqualTo("> "));
    }

    [Test]
    public void GetPointerTextForIndex_WhenNotSelected_ReturnsSpaces()
    {
        Assert.That(ShopController.GetPointerTextForIndex(1, 0), Is.EqualTo("  "));
        Assert.That(ShopController.GetPointerTextForIndex(0, 1), Is.EqualTo("  "));
    }

    [Test]
    public void GetPointerTextForIndex_OnlyOneIndexShowsPointerForGivenSelectedIndex()
    {
        int selectedIndex = 2;
        int itemCount = 5;
        int pointerCount = 0;
        for (int i = 0; i < itemCount; i++)
        {
            string text = ShopController.GetPointerTextForIndex(i, selectedIndex);
            if (text == "> ")
                pointerCount++;
        }
        Assert.That(
            pointerCount,
            Is.EqualTo(1),
            "Exactly one option should show the pointer for a given selectedIndex"
        );
    }
}
