# Pixata.SimilarityChooser [![Pixata.SimilarityChooser Nuget package](https://img.shields.io/nuget/v/Pixata.SimilarityChooser)](https://www.nuget.org/packages/Pixata.SimilarityChooser/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.SimilarityChooser/Icon/recherche.png "Pixata") 

A utility that checks for similar items. Useful for when your users don't bother checking to see if the data they want already exists, and add a duplicate. The Similarity Chooser looks through your existing items and lets you know which may match.

SQL Server offers something similar with its free-text search, but this is very limited, and is restricted to known word form variants, such as colour/color and so on. It does not match typos, which makes it fairly useless when dealing with user input.

The similarity chooser uses a Metaphone search, which means it matches words phonetically. This means that if the user entered "humus" it would match "hummous," or one of various other variations. This makes it much more powerful than free-text searches.

## Set up
Before you can look for matches, you need to set up the chooser. This is done by creating a collection of `MatchingEntityOverview` objects and passing it to the `CreateMap` method...

```c#
SimilarEntityChooser<Products> chooser = SimilarEntityChooser<Product>
  .CreateMap(appDbContext.Products
    .Select(p => new MatchingEntityOverview<Products> {
      ID = p.ProductID,
      MatchText = p.ProductName,
      Entity = p
    }));
```

Once you have the chooser set up, you can query it...

```c#
IEnumerable<Products> syrups = chooser
  .FindSimilar("syrup")
  .Select(meo => meo.Entity);
```

You can then display the choice to your users as you wish.

If you are working in a stateful environment, such as a desktop or Blazor application, where the collection can be held in memory for multiple uses, the code above is fine. However, in a stateless environment, such as a regular web application, you would need to create the map on each request. In that case, you can simplify the code by wapping it into one call...

```c#
IEnumerable<Products> syrups = SimilarEntityChooser<Products>
  .CreateMap(appDbContext.Products
    .Select(p => new MatchingEntityOverview<Products> { 
      ID = p.ProductID, 
      MatchText = p.ProductName, 
      Entity = p 
    }))
  .FindSimilar("syrup")
  .Select(meo => meo.Entity);
```

## Limitations
There are two points to keep in mind when using the similarity chooser...

1. As the matching is done phonetically, the results are not always what the user might expect. For example, on a groceries site where we use this, entering "pasta" brings up an unexpected result...

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.SimilarityChooser/Icon/FalseMatch.png "A false match") 

I'm not an expert in linguists, but must of the time I can work out why a match appears, but it can confuse people. Generally, the longer the search text they enter, the better the matches, but there will always be some unexpected results.

2. The chooser isn't too good at matching plurals. For example, if you search for "apple", it will not match "apples." This is a known limitation, and something I'd like to address at some point.