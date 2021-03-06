using OOPsIDidItAgain._06.MinimizingExceptions.Web.Domain;
using OOPsIDidItAgain._06.MinimizingExceptions.Web.Domain.PostAddItemToCartListeners;
using OOPsIDidItAgain._06.MinimizingExceptions.Web.Shared;

namespace OOPsIDidItAgain._06.MinimizingExceptions.Web.Handlers
{
    public record AddItemToCartRequest(CartId CartId, ItemId ItemId, int Quantity) : IRequest<Either<Error, Unit>>;
    
    public class AddItemToCartHandler : IRequestHandler<AddItemToCartRequest, Either<Error, Unit>>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IItemSaleRuleRepository _itemSaleRuleRepository;
        
        private readonly IPostAddItemToCartListener _listener;

        public AddItemToCartHandler(
            ICartRepository cartRepository,
            IItemRepository itemRepository,
            IItemSaleRuleRepository itemSaleRuleRepository,
            IPostAddItemToCartListener listener)
        {
            _cartRepository = cartRepository;
            _itemRepository = itemRepository;
            _itemSaleRuleRepository = itemSaleRuleRepository;
            _listener = listener;
        }

        public Either<Error, Unit> Handle(AddItemToCartRequest input)
        {
            var maybeCart = _cartRepository.Get(input.CartId);
            if (!maybeCart.TryGetValue(out var cart))
            {
                return Either.Left<Error, Unit>(new Error.NotFound("Couldn't find the cart"));
            }
            
            var item = _itemRepository.Get(input.ItemId);
            if (item is null)
            {
                return Either.Left<Error, Unit>(new Error.NotFound("Couldn't find the item"));
            }

            var rules = _itemSaleRuleRepository.GetForItem(item.Id);
            var rulesResult = rules.Validate(cart, item, input.Quantity);
            
            
            if (rulesResult is Either<Error, Unit>.Left)
            {
                return rulesResult;
            }
            
            var cartItemResult = cart.AddItemToCart(item, input.Quantity);
            
            if (cartItemResult is Either<Error, CartItem>.Left cartItemErrorResult)
            {
                return Either.Left<Error, Unit>(cartItemErrorResult.Value);
            }

            _cartRepository.Save(cart);
            
            _listener.OnAdded(cart, item, ((Either<Error, CartItem>.Right)cartItemResult).Value);

            return Either.Right<Error, Unit>(Unit.Instance);
        }
    }
}