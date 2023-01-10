using Microsoft.Xna.Framework;

namespace FEZUG.Features
{
    public interface IFezugFeature
    {
        void Update(GameTime gameTime);
        void Draw(GameTime gameTime);
    }
}
