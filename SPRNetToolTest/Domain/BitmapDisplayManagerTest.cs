using ArtWiz.Domain;
using ArtWiz.Domain.Base;

namespace ArtWizTest.Domain
{
    internal class BitmapDisplayManagerTest
    {
        private string sprFilePath = "Resources\\test.spr";
        private string pngFilePath = "Resources\\test.png";
        private ISprEditorBitmapDisplayManager bitmapDisplayManager;

        [SetUp]
        public void Setup()
        {
            bitmapDisplayManager = new SprEditorBitmapDisplayManager();
            bool isNeedToOpenSprFile = GetType()
                .GetMethod(TestContext.CurrentContext.Test.MethodName ?? "")?
                .GetCustomAttributes(true)
                .Any(it => it is NeedToOpenSprFileAttribute) ?? false;

            //if (isNeedToOpenSprFile)
            //{
            //    bitmapDisplayManager.OpenBitmapFromFile(sprFilePath);
            //}

        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test, NeedToOpenSprFile]
        public void test_InsertFrame()
        {
        }
    }

    internal class NeedToOpenSprFileAttribute : Attribute { }
}
