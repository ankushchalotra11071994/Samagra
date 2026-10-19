//Core hooks, asked in almost every interview: useState → 
// useEffect → useRef → 
// useContext → useReducer → useMemo → useCallback → useLayoutEffect
import React, { useEffect, useState, useRef } from "react";

function HooksPractice() {
    const nameRef = useRef(null);
    const [count, setcount] = useState(1);

    const [callback, depemdency] = useEffect()
    const chnagenumber = (val) => {
        setcount(count => count + val)
        alert("Name: " + nameRef.Current.Value);

    }

    //useref


    //use effect
    useEffect(() => {
        const id = setInterval(() => fetchData(), 5000);   // mount par shuru
        return () => clearInterval(id);                    // unmount par band
    }, []);

    useEffect(() => { console.log("1. Kuch nahi likha"); });
    useEffect(() => { console.log("2. null"); }, null);
    useEffect(() => { console.log("3. Khaali array"); }, []);
    useEffect(() => { console.log("4. count:", count); }, [count]);
    // useEffect(() => {
    //     let ignore = false;

    //     axios
    //         .get("https://jsonplaceholder.typicode.com/users")
    //         .then(res => {
    //             if (!ignore) {
    //                 setEmployees(res.data);
    //             }
    //         });

    //     return () => {
    //         ignore = true;   // page chhoda → response ko ignore karo
    //     };
    // }, []);




    return (
        <>
            <h1> {count} </h1>
            <input ref={nameRef} placeholder="Enter name" />
            <button onClick={() => chnagenumber(1)}   > Increse</button>
            <button onClick={() => chnagenumber(-1)}  > decrese</button>
        </>

    )
}


export default HooksPractice