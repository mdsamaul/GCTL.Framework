# IRepository&lt;T&gt; vs IGenericRepository&lt;T&gt; — কোনটা কখন ব্যবহার করবেন

এই প্রজেক্টে দুইটা জেনেরিক রিপোজিটরি ইন্টারফেস আছে। দুটোই EF Core দিয়ে
CRUD করে, দুটোই `GctlHrmContext` ব্যবহার করে — কিন্তু এদের অস্তিত্বের কারণ
আলাদা।

## সংক্ষেপে

| | `IRepository<T>` | `IGenericRepository<T>` |
|---|---|---|
| আপনার প্রজেক্টে কোথায় দেখা যায় | সব জায়গায় — মূল/legacy প্যাটার্ন | নতুন/নির্দিষ্ট কিছু module (যেমন OFRApprovalService) |
| Registration | `services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>))` | `services.AddScoped(typeof(IGenericRepository<>), typeof(GcGenericRepository<>))` |
| Method নামকরণ | sync-first (`GetAll()`, `Add()`, ... + async ভ্যারিয়েন্ট) | একই রকম, কিন্তু `Any`/`AnyAsync` বাড়তি আছে |
| কবে ব্যবহার করবেন | **ডিফল্ট চয়েস** — নতুন যেকোনো Service/Controller লিখলে এটাই ব্যবহার করুন | শুধু তখনই, যখন কোনো নির্দিষ্ট module (যেমন OFR Approval) ইতিমধ্যে এটার উপর নির্ভর করে লেখা, অথবা আপনি সচেতনভাবে `AnyAsync`-এর মতো এক্সট্রা মেথড চান |

## বিস্তারিত

### `IRepository<T>` — আপনার মূল প্যাটার্ন
আপনার পুরনো `GCTL_ERP_HRM` প্রজেক্টে প্রায় সব Controller/Service এটাই ব্যবহার
করত (`OfficialInfoController` সহ)। এই Framework প্রজেক্টেও এটাই এখন পর্যন্ত
প্রধান রিপোজিটরি — `GenericRepository<T>` ক্লাস এটা implement করে।

```csharp
public class SomeService : AppService<SomeEntity>, ISomeService
{
    private readonly IRepository<SomeEntity> _repo;

    public SomeService(IRepository<SomeEntity> repo) : base(repo)
    {
        _repo = repo;
    }
}
```

**নতুন যেকোনো module লিখতে গেলে এটাই default choice হওয়া উচিত** — কারণ
আপনার বাকি সব পুরনো কোডের সাথে এটাই সামঞ্জস্যপূর্ণ।

### `IGenericRepository<T>` — নির্দিষ্ট module-এর জন্য
আপনার পাঠানো `OFRApprovalService` কোডে এটা ব্যবহার হয়েছে:

```csharp
private readonly IGenericRepository<CF_OperationalFundRequisitionEntry> _repository;
```

এই ইন্টারফেসটা মূলত `IRepository<T>`-এরই সমান কাজ করে, বাড়তি
`Any`/`AnyAsync` মেথড সহ। এটা রাখা হয়েছে যাতে আপনার `OFRApprovalService`-এর
মতো module — যেগুলো ইতিমধ্যে এই ইন্টারফেসের উপর নির্ভর করে লেখা — কোনো
পরিবর্তন ছাড়াই এই Framework প্রজেক্টে কাজ করে।

**নতুন module লেখার সময় এটা বেছে নেবেন শুধু যদি:**
- সেই module-টা কোনো পুরনো কোড থেকে পোর্ট করছেন যেটা আগে থেকেই
  `IGenericRepository<T>` ব্যবহার করত, অথবা
- আপনার `AnyAsync(predicate)`-এর মতো existence-check মেথড দরকার এবং
  `IRepository<T>`-এ সেটা নেই (`IRepository<T>`-এ আপনাকে
  `FindBy(...).Any()` দিয়ে ম্যানুয়ালি করতে হবে)

## একসাথে ব্যবহার করা যাবে?

হ্যাঁ — দুটো আলাদা ইন্টারফেস, আলাদা DI registration, কোনো conflict নেই।
একই Service ক্লাসে চাইলে দুটোই inject করা যায় (আপনার OFRApprovalService
আসলে সেটাই করছে — `IRepository<>` থেকে না, বরং base class-এর জন্য একটা
আলাদা `IGenericRepository<T>` নিচ্ছে)।

## আমার সাজেশন

নতুন কাজের জন্য **`IRepository<T>` কেই default রাখুন** — এটাই আপনার পুরো
কোডবেসে সবচেয়ে বেশি ব্যবহৃত এবং সবচেয়ে ভালোভাবে টেস্ট করা প্যাটার্ন।
`IGenericRepository<T>` শুধু legacy module পোর্ট করার সময় বা বিশেষ কোনো
`AnyAsync`-এর মতো প্রয়োজন হলে ব্যবহার করুন।
