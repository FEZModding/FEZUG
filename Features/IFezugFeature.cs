using Microsoft.Xna.Framework;

namespace FEZUG.Features
{
    public interface IFezugFeature
    {
        void Initialize();
        void Update(GameTime gameTime);
        void Draw(GameTime gameTime);
    }
}
