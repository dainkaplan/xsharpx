﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XSharpx
{

    public class FreeDict<A, B, C>
    {
        public readonly Either<C, Either<Func<Tuple<A, B, FreeDict<A, B, C>>>, Func<Tuple<A, Func<B,FreeDict<A, B, C>>>>>> maybeGetPushMore;
       
        public FreeDict(C c) //Done
        {
            maybeGetPushMore = Either<C, Either<Func<Tuple<A, B, FreeDict<A, B, C>>>, Func<Tuple<A, Func<B, FreeDict<A, B, C>>>>>>.Left(c);
        }

        public FreeDict(Func<Tuple<A, B, FreeDict<A, B, C>>> f) //Add+More
        {
            maybeGetPushMore = Either<C, Either<Func<Tuple<A, B, FreeDict<A, B, C>>>, Func<Tuple<A, Func<B,FreeDict<A, B, C>>>>>>
                 .Right(Either<Func<Tuple<A, B, FreeDict<A, B, C>>>, Func<Tuple<A, Func<B, FreeDict<A, B, C>>>>>.Left(f));
        }

        public FreeDict(Func<Tuple<A, Func<B,FreeDict<A, B, C>>>> f)//Get+More
        {
            maybeGetPushMore = Either<C, Either<Func<Tuple<A, B, FreeDict<A, B, C>>>, Func<Tuple<A, Func<B, FreeDict<A, B, C>>>>>>
                .Right(Either<Func<Tuple<A, B, FreeDict<A, B, C>>>, Func<Tuple<A, Func<B, FreeDict<A, B, C>>>>>.Right(f));
        }

        public static FreeDict<A, B, Unit> Add(A a, B b)
        {
            return new FreeDict<A, B, Unit>(()=>new Tuple<A,B,FreeDict<A,B,Unit>>(a, b, new FreeDict<A,B,Unit>(new Unit())));
        }

        public static FreeDict<A, B, B> Get(A a)
        {
            return new FreeDict<A,B,B>( ()=> new Tuple<A,Func<B,FreeDict<A,B,B>>>(a, (b)=>new FreeDict<A,B,B>(b)));
        }

        //UNSAFE
        public C Run
        {
            get
            {
                return RunRec(new Dictionary<A, B>());
            }
        }

        private C RunRec(Dictionary<A, B> dict)
        {
            return maybeGetPushMore.Swap.Reduce(mf => mf.Fold(addf =>
            {
                var t = addf();
                dict.Add(t.Item1, t.Item2);
                return t.Item3.RunRec(dict);
            },
            getf =>
            {
                var t = getf();
                var b = dict[t.Item1];
                return t.Item2(b).RunRec(dict);
            }));
        }
    }

    public static class FreeDictExt
    {
        public static FreeDict<A, B, D> Select<A, B, C, D>(this FreeDict<A, B, C> fd, Func<C, D> f)
        {
            return fd.SelectMany(c => new FreeDict<A, B, D>(f(c)));
        }

        public static FreeDict<A, B, E> SelectMany<A, B, C, D, E>(this FreeDict<A, B, C> fd, Func<C, FreeDict<A, B, D>> bind, Func<C, D, E> select)
        {
            return fd.SelectMany(c => bind(c).Select(d => select(c, d)));
        }

        public static FreeDict<A, B, D> SelectMany<A, B, C, D>(this FreeDict<A, B, C> fd, Func<C, FreeDict<A, B, D>> bind)
        {
            return fd.maybeGetPushMore.Fold(c => bind(c), mf => mf.Fold(addf =>
                {
                    var t = addf();
                    return new FreeDict<A, B, D>(() => new Tuple<A, B, FreeDict<A, B, D>>(t.Item1, t.Item2, t.Item3.SelectMany(c => bind(c))));
                }, getf =>
                {
                    var t = getf();
                    return new FreeDict<A, B, D>(() => new Tuple<A, Func<B, FreeDict<A, B, D>>>(t.Item1, (b) => t.Item2(b).SelectMany(c => bind(c))));
                }
           ));
        }
    }
     
}
