namespace Tests

open NUnit.Framework
open DiffSharp

[<TestFixture>]
type TestTensor () =

    [<SetUp>]
    member this.Setup () =
        ()

    [<Test>]
    member this.TestTensorCreate () =
        let t0 = Tensor.Create(1.)
        let t0Shape = t0.Shape
        let t0Dim = t0.Dim
        let t0ShapeCorrect = [||]
        let t0DimCorrect = 0

        let t1 = Tensor.Create([1.; 2.; 3.])
        let t1Shape = t1.Shape
        let t1Dim = t1.Dim
        let t1ShapeCorrect = [|3|]
        let t1DimCorrect = 1

        let t2 = Tensor.Create([[1.; 2.; 3.]; [4.; 5.; 6.]])
        let t2Shape = t2.Shape
        let t2Dim = t2.Dim
        let t2ShapeCorrect = [|2; 3|]
        let t2DimCorrect = 2

        let t3 = Tensor.Create([[[1.; 2.; 3.]; [4.; 5.; 6.]]])
        let t3Shape = t3.Shape
        let t3Dim = t3.Dim
        let t3ShapeCorrect = [|1; 2; 3|]
        let t3DimCorrect = 3

        let t4 = Tensor.Create([[[[1.; 2.]]]])
        let t4Shape = t4.Shape
        let t4Dim = t4.Dim
        let t4ShapeCorrect = [|1; 1; 1; 2|]
        let t4DimCorrect = 4

        Assert.AreEqual(t0Shape, t0ShapeCorrect)
        Assert.AreEqual(t1Shape, t1ShapeCorrect)
        Assert.AreEqual(t2Shape, t2ShapeCorrect)
        Assert.AreEqual(t3Shape, t3ShapeCorrect)
        Assert.AreEqual(t4Shape, t4ShapeCorrect)
        Assert.AreEqual(t0Dim, t0DimCorrect)
        Assert.AreEqual(t1Dim, t1DimCorrect)
        Assert.AreEqual(t2Dim, t2DimCorrect)
        Assert.AreEqual(t3Dim, t3DimCorrect)
        Assert.AreEqual(t4Dim, t4DimCorrect)

    [<Test>]
    member this.TestTensorToArray () =
        let a = array2D [[1.; 2.]; [3.; 4.]]
        let t = Tensor.Create(a)
        let v = t.ToArray()
        Assert.AreEqual(a, v)

    [<Test>]
    member this.TestTensorToString () =
        let t0 = Tensor.Create(2.)
        let t1 = Tensor.Create([[2.]; [2.]])
        let t2 = Tensor.Create([[[2.; 2.]]])
        let t3 = Tensor.Create([[1.;2.]; [3.;4.]])
        let t4 = Tensor.Create([[[[1.]]]])
        let t0String = t0.ToString()
        let t1String = t1.ToString()
        let t2String = t2.ToString()
        let t3String = t3.ToString()
        let t4String = t4.ToString()
        let t0StringCorrect = "Tensor 2.0f"
        let t1StringCorrect = "Tensor [[2.0f]; [2.0f]]"
        let t2StringCorrect = "Tensor [[[2.0f; 2.0f]]]"
        let t3StringCorrect = "Tensor [[1.0f; 2.0f]; [3.0f; 4.0f]]"
        let t4StringCorrect = "Tensor [[[[1.0f]]]]"
        Assert.AreEqual(t0String, t0StringCorrect)
        Assert.AreEqual(t1String, t1StringCorrect)
        Assert.AreEqual(t2String, t2StringCorrect)
        Assert.AreEqual(t3String, t3StringCorrect)
        Assert.AreEqual(t4String, t4StringCorrect)

    [<Test>]
    member this.TestTensorCompare () =
        let t1 = Tensor.Create(-1.)
        let t2 = Tensor.Create(1.)
        let t3 = Tensor.Create(1.)
        let t1t2Less = t1 < t2
        let t1t2LessCorrect = true
        let t1t2Equal = t1 = t2
        let t1t2EqualCorrect = false
        let t2t3Equal = t2 = t3
        let t2t3EqualCorrect = true

        Assert.AreEqual(t1t2Less, t1t2LessCorrect)
        Assert.AreEqual(t1t2Equal, t1t2EqualCorrect)
        Assert.AreEqual(t2t3Equal, t2t3EqualCorrect)

    [<Test>]
    member this.TestTensorLtTT () =
        let t1 = Tensor.Create([1.; 2.; 3.; 5.])
        let t2 = Tensor.Create([1.; 3.; 5.; 4.])
        let t1t2Lt = t1.Lt(t2)
        let t1t2LtCorrect = Tensor.Create([0.; 1.; 1.; 0.])

        Assert.AreEqual(t1t2Lt, t1t2LtCorrect)

    [<Test>]
    member this.TestTensorLeTT () =
        let t1 = Tensor.Create([1.; 2.; 3.; 5.])
        let t2 = Tensor.Create([1.; 3.; 5.; 4.])
        let t1t2Le = t1.Le(t2)
        let t1t2LeCorrect = Tensor.Create([1.; 1.; 1.; 0.])

        Assert.AreEqual(t1t2Le, t1t2LeCorrect)

    [<Test>]
    member this.TestTensorGtTT () =
        let t1 = Tensor.Create([1.; 2.; 3.; 5.])
        let t2 = Tensor.Create([1.; 3.; 5.; 4.])
        let t1t2Gt = t1.Gt(t2)
        let t1t2GtCorrect = Tensor.Create([0.; 0.; 0.; 1.])

        Assert.AreEqual(t1t2Gt, t1t2GtCorrect)

    [<Test>]
    member this.TestTensorGeTT () =
        let t1 = Tensor.Create([1.; 2.; 3.; 5.])
        let t2 = Tensor.Create([1.; 3.; 5.; 4.])
        let t1t2Ge = t1.Ge(t2)
        let t1t2GeCorrect = Tensor.Create([1.; 0.; 0.; 1.])

        Assert.AreEqual(t1t2Ge, t1t2GeCorrect)

    [<Test>]
    member this.TestTensorAddTT () =
        let t1 = Tensor.Create([1.; 2.]) + Tensor.Create([3.; 4.])
        let t1Correct = Tensor.Create([4.; 6.])

        let t2 = Tensor.Create([1.; 2.]) + Tensor.Create(5.)
        let t2Correct = Tensor.Create([6.; 7.])

        let t3 = Tensor.Create([1.; 2.]) + 5.f
        let t3Correct = Tensor.Create([6.; 7.])

        let t4 = Tensor.Create([1.; 2.]) + 5.
        let t4Correct = Tensor.Create([6.; 7.])

        let t5 = Tensor.Create([1.; 2.]) + 5
        let t5Correct = Tensor.Create([6.; 7.])

        Assert.AreEqual(t1, t1Correct)
        Assert.AreEqual(t2, t2Correct)
        Assert.AreEqual(t3, t3Correct)
        Assert.AreEqual(t4, t4Correct)
        Assert.AreEqual(t5, t5Correct)

    [<Test>]
    member this.TestTensorStackTs () =
        let t0a = Tensor.Create(1.)
        let t0b = Tensor.Create(3.)
        let t0c = Tensor.Create(5.)
        let t0 = Tensor.Stack([t0a;t0b;t0c])
        let t0Correct = Tensor.Create([1.;3.;5.])

        let t1a = Tensor.Create([1.; 2.])
        let t1b = Tensor.Create([3.; 4.])
        let t1c = Tensor.Create([5.; 6.])
        let t1 = Tensor.Stack([t1a;t1b;t1c])
        let t1Correct = Tensor.Create([[1.;2.];[3.;4.];[5.;6.]])

        Assert.AreEqual(t0, t0Correct)
        Assert.AreEqual(t1, t1Correct)

    [<Test>]
    member this.TestTensorUnstackT () =
        let t0a = Tensor.Create(1.)
        let t0b = Tensor.Create(3.)
        let t0c = Tensor.Create(5.)
        let t0Correct = [t0a;t0b;t0c]
        let t0 = Tensor.Stack(t0Correct).Unstack()

        let t1a = Tensor.Create([1.; 2.])
        let t1b = Tensor.Create([3.; 4.])
        let t1c = Tensor.Create([5.; 6.])
        let t1Correct = [t1a;t1b;t1c]
        let t1 = Tensor.Stack(t1Correct).Unstack()

        Assert.AreEqual(t0, t0Correct)
        Assert.AreEqual(t1, t1Correct)

    [<Test>]
    member this.TestTensorAddT2T1 () =
        let t1 = Tensor.Create([[1.; 2.]; [3.; 4.]]) + Tensor.Create([5.; 6.])
        let t1Correct = Tensor.Create([[6.; 8.]; [8.; 10.]])

        Assert.AreEqual(t1, t1Correct)

    [<Test>]
    member this.TestTensorSubTT () =
        let t1 = Tensor.Create([1.; 2.]) - Tensor.Create([3.; 4.])
        let t1Correct = Tensor.Create([-2.; -2.])

        let t2 = Tensor.Create([1.; 2.]) - Tensor.Create(5.)
        let t2Correct = Tensor.Create([-4.; -3.])

        let t3 = Tensor.Create([1.; 2.]) - 5.f
        let t3Correct = Tensor.Create([-4.; -3.])

        let t4 = 5. - Tensor.Create([1.; 2.])
        let t4Correct = Tensor.Create([4.; 3.])

        Assert.AreEqual(t1, t1Correct)
        Assert.AreEqual(t2, t2Correct)
        Assert.AreEqual(t3, t3Correct)
        Assert.AreEqual(t4, t4Correct)

    [<Test>]
    member this.TestTensorMulTT () =
        let t1 = Tensor.Create([1.; 2.]) * Tensor.Create([3.; 4.])
        let t1Correct = Tensor.Create([3.; 8.])

        let t2 = Tensor.Create([1.; 2.]) * Tensor.Create(5.)
        let t2Correct = Tensor.Create([5.; 10.])

        let t3 = Tensor.Create([1.; 2.]) * 5.f
        let t3Correct = Tensor.Create([5.; 10.])

        let t4 = 5. * Tensor.Create([1.; 2.])
        let t4Correct = Tensor.Create([5.; 10.])

        Assert.AreEqual(t1, t1Correct)
        Assert.AreEqual(t2, t2Correct)
        Assert.AreEqual(t3, t3Correct)
        Assert.AreEqual(t4, t4Correct)

    [<Test>]
    member this.TestTensorDivTT () =
        let t1 = Tensor.Create([1.; 2.]) / Tensor.Create([3.; 4.])
        let t1Correct = Tensor.Create([0.333333; 0.5])

        let t2 = Tensor.Create([1.; 2.]) / Tensor.Create(5.)
        let t2Correct = Tensor.Create([0.2; 0.4])

        let t3 = Tensor.Create([1.; 2.]) / 5.f
        let t3Correct = Tensor.Create([0.2; 0.4])

        let t4 = 5. / Tensor.Create([1.; 2.])
        let t4Correct = Tensor.Create([5.; 2.5])

        Assert.True(t1.ApproximatelyEqual(t1Correct))
        Assert.True(t2.ApproximatelyEqual(t2Correct))
        Assert.True(t3.ApproximatelyEqual(t3Correct))
        Assert.True(t4.ApproximatelyEqual(t4Correct))

    [<Test>]
    member this.TestTensorPowTT () =
        let t1 = Tensor.Create([1.; 2.]) ** Tensor.Create([3.; 4.])
        let t1Correct = Tensor.Create([1.; 16.])

        let t2 = Tensor.Create([1.; 2.]) ** Tensor.Create(5.)
        let t2Correct = Tensor.Create([1.; 32.])

        let t3 = Tensor.Create(5.) ** Tensor.Create([1.; 2.])
        let t3Correct = Tensor.Create([5.; 25.])

        Assert.AreEqual(t1, t1Correct)
        Assert.AreEqual(t2, t2Correct)
        Assert.AreEqual(t3, t3Correct)

    [<Test>]
    member this.TestTensorMatMulT2T2 () =
        let t1 = Tensor.Create([[8.0766; 3.3030; 2.1732; 8.9448; 1.1028];
                                [4.1215; 4.9130; 5.2462; 4.2981; 9.3622];
                                [7.4682; 5.2166; 5.1184; 1.9626; 0.7562]])
        let t2 = Tensor.Create([[5.1067; 0.0681];
                                [7.4633; 3.6027];
                                [9.0070; 7.3012];
                                [2.6639; 2.8728];
                                [7.9229; 2.3695]])

        let t3 = Tensor.MatMul(t1, t2)
        let t3Correct = Tensor.Create([[118.0367; 56.6266];
                                        [190.5926; 90.8155];
                                        [134.3925; 64.1030]])

        Assert.True(t3.ApproximatelyEqual(t3Correct))

    [<Test>]
    member this.TestTensorNegT () =
        let t1 = Tensor.Create([1.; 2.; 3.])
        let t1Neg = -t1
        let t1NegCorrect = Tensor.Create([-1.; -2.; -3.])

        Assert.AreEqual(t1Neg, t1NegCorrect)

    [<Test>]
    member this.TestTensorSumT () =
        let t1 = Tensor.Create([1.; 2.; 3.])
        let t1Sum = t1.Sum()
        let t1SumCorrect = Tensor.Create(6.)

        let t2 = Tensor.Create([[1.; 2.]; [3.; 4.]])
        let t2Sum = t2.Sum()
        let t2SumCorrect = Tensor.Create(10.)

        Assert.AreEqual(t1Sum, t1SumCorrect)
        Assert.AreEqual(t2Sum, t2SumCorrect)

    [<Test>]
    member this.TestTensorSumT2Dim0 () =
        let t1 = Tensor.Create([[1.; 2.]; [3.; 4.]])
        let t1Sum = t1.SumT2Dim0()
        let t1SumCorrect = Tensor.Create([4.; 6.])

        Assert.AreEqual(t1Sum, t1SumCorrect)
    
    [<Test>]
    member this.TestTensorTransposeT2 () =
        let t1 = Tensor.Create([[1.; 2.; 3.]; [4.; 5.; 6.]])
        let t1Transpose = t1.Transpose()
        let t1TransposeCorrect = Tensor.Create([[1.; 4.]; [2.; 5.]; [3.; 6.]])

        let t2 = Tensor.Create([[1.; 2.]; [3.; 4.]])
        let t2TransposeTranspose = t2.Transpose().Transpose()
        let t2TransposeTransposeCorrect = t2

        Assert.AreEqual(t1Transpose, t1TransposeCorrect)
        Assert.AreEqual(t2TransposeTranspose, t2TransposeTransposeCorrect)

    [<Test>]
    member this.TestTensorSignT () =
        let t1 = Tensor.Create([-1.; -2.; 0.; 3.])
        let t1Sign = t1.Sign()
        let t1SignCorrect = Tensor.Create([-1.; -1.; 0.; 1.])

        Assert.AreEqual(t1Sign, t1SignCorrect)

    [<Test>]
    member this.TestTensorAbsT () =
        let t1 = Tensor.Create([-1.; -2.; 0.; 3.])
        let t1Abs = t1.Abs()
        let t1AbsCorrect = Tensor.Create([1.; 2.; 0.; 3.])

        Assert.AreEqual(t1Abs, t1AbsCorrect)

    [<Test>]
    member this.TestTensorReluT () =
        let t1 = Tensor.Create([-1.; -2.; 0.; 3.; 10.])
        let t1Relu = t1.Relu()
        let t1ReluCorrect = Tensor.Create([0.; 0.; 0.; 3.; 10.])

        Assert.AreEqual(t1Relu, t1ReluCorrect)

    [<Test>]
    member this.TestTensorExpT () =
        let t1 = Tensor.Create([0.9139; -0.5907;  1.9422; -0.7763; -0.3274])
        let t1Exp = t1.Exp()
        let t1ExpCorrect = Tensor.Create([2.4940; 0.5539; 6.9742; 0.4601; 0.7208])

        Assert.True(t1Exp.ApproximatelyEqual(t1ExpCorrect))

    [<Test>]
    member this.TestTensorLogT () =
        let t1 = Tensor.Create([0.1285; 0.5812; 0.6505; 0.3781; 0.4025])
        let t1Log = t1.Log()
        let t1LogCorrect = Tensor.Create([-2.0516; -0.5426; -0.4301; -0.9727; -0.9100])

        Assert.True(t1Log.ApproximatelyEqual(t1LogCorrect))

    [<Test>]
    member this.TestTensorSqrtT () =
        let t1 = Tensor.Create([54.7919; 70.6440; 16.0868; 74.5486; 82.9318])
        let t1Sqrt = t1.Sqrt()
        let t1SqrtCorrect = Tensor.Create([7.4022; 8.4050; 4.0108; 8.6342; 9.1067])

        Assert.True(t1Sqrt.ApproximatelyEqual(t1SqrtCorrect))   